// Users model class
//
// Part of ASP.NET osa framework  www.osalabs.com/osafw/asp.net
// (c) 2009-2021 Oleg Savchuk www.osalabs.com

//if you use Roles - uncomment define isRoles here
//#define isRoles

using OtpNet;
using QRCoder;
using System;
using System.Collections;
using System.Text.RegularExpressions;
using static BCrypt.Net.BCrypt;

namespace osafw;

public class Users : FwModel
{
    // ACL constants
    public const int ACL_VISITOR = 0; //non-logged visitor
    public const int ACL_MEMBER = 1; //min access level for users
    public const int ACL_EMPLOYEE = 50;
    public const int ACL_MANAGER = 80;
    public const int ACL_ADMIN = 90;
    public const int ACL_SITEADMIN = 100;

    public const string PERM_COOKIE_NAME = "osafw_perm";
    public const int PERM_COOKIE_DAYS = 356;

    private readonly string table_menu_items = "menu_items";
    private readonly string table_users_cookies = "users_cookies";

    public Users() : base()
    {
        table_name = "users";
        csv_export_fields = "id fname lname email add_time";
        csv_export_headers = "id,First Name,Last Name,Email,Registered";
    }

    #region standard one/add/update overrides
    public Hashtable oneByEmail(string email)
    {
        Hashtable where = new();
        where["email"] = email;
        return db.row(table_name, where);
    }

    /// <summary>
    /// return full user name - First Name Last Name
    /// </summary>
    /// <param name="id">Object type because if upd_users_id could be null</param>
    /// <returns></returns>
    public new string iname(object id)
    {
        string result = "";

        int iid = Utils.toInt(id);
        if (iid > 0)
        {
            var item = one(iid);
            result = item["fname"] + "  " + item["lname"];
        }

        return result;
    }

    // check if user exists for a given email
    public override bool isExists(object uniq_key, int not_id)
    {
        return isExistsByField(uniq_key, not_id, "email");
    }

    public override int add(Hashtable item)
    {
        if (!item.ContainsKey("access_level"))
            item["access_level"] = Users.ACL_MEMBER;

        if (!item.ContainsKey("pwd"))
            item["pwd"] = Utils.getRandStr(8); // generate password
        item["pwd"] = this.hashPwd((string)item["pwd"]);

        // set ui_theme/ui_mode form the config if not set
        if (!item.ContainsKey("ui_theme"))
            item["ui_theme"] = Utils.toInt(fw.config("ui_theme"));
        if (!item.ContainsKey("ui_mode"))
            item["ui_mode"] = Utils.toInt(fw.config("ui_mode"));

        return base.add(item);
    }

    public override bool update(int id, Hashtable item)
    {
        if (id == 0) return false;//no anonymous updates

        if (item.ContainsKey("pwd"))
            item["pwd"] = this.hashPwd((string)item["pwd"]);
        return base.update(id, item);
    }

    protected override string getOrderBy()
    {
        return "fname, lname";
    }

    // return standard list of id,iname where status=0 order by iname
    public override DBList list(IList statuses = null)
    {
        if (statuses == null)
            statuses = new ArrayList() { STATUS_ACTIVE };
        return base.list(statuses);
    }

    public override ArrayList listSelectOptions(Hashtable def = null)
    {
        string sql = "select id, fname+' '+lname as iname from " + db.qid(table_name) + " where status=@status order by " + getOrderBy();
        return db.arrayp(sql, DB.h("status", STATUS_ACTIVE));
    }
    #endregion

    #region Work with Passwords/MFA
    /// <summary>
    /// performs any required password cleaning (for now - just limit pwd length at 32 and trim)
    /// </summary>
    /// <param name="plain_pwd">non-encrypted plain pwd</param>
    /// <param name="trim_at">max length</param>
    /// <returns>clean plain pwd</returns>
    public string cleanPwd(string plain_pwd, int trim_at = 32)
    {
        return plain_pwd[..Math.Min(trim_at, plain_pwd.Length)].Trim();
    }

    /// <summary>
    /// generate password hash from plain password
    /// </summary>
    /// <param name="plain_pwd">plain pwd</param>
    /// <param name="trim_at">max length to trim plain pwd before hashing</param>
    /// <returns>hash using https://github.com/BcryptNet/bcrypt.net </returns>
    public string hashPwd(string plain_pwd, int trim_at = 32)
    {
        try
        {
            return EnhancedHashPassword(cleanPwd(plain_pwd, trim_at));
        }
        catch (Exception)
        {
        }
        return "";
    }

    /// <summary>
    /// return true if plain password has the same hash as provided
    /// </summary>
    /// <param name="plain_pwd">plain pwd from user input</param>
    /// <param name="pwd_hash">password hash previously generated by hashPwd</param>
    /// <returns></returns>
    public bool checkPwd(string plain_pwd, string pwd_hash, int trim_at = 32)
    {
        try
        {
            return EnhancedVerify(cleanPwd(plain_pwd, trim_at), pwd_hash);
        }
        catch (Exception)
        {
        }
        return false;
    }

    /// <summary>
    /// generate reset token, save to users and send pwd reset link to the user
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool sendPwdReset(int id)
    {
        var pwd_reset_token = Utils.getRandStr(50);

        Hashtable item = new()
        {
            {"pwd_reset", this.hashPwd(pwd_reset_token, 50)},
            {"pwd_reset_time", DB.NOW}
        };
        this.update(id, item);

        var user = this.one(id);
        user["pwd_reset_token"] = pwd_reset_token;

        return fw.sendEmailTpl((string)user["email"], "email_pwd.txt", user);
    }

    /// <summary>
    /// evaluate password's stength and return a score (>60 good, >80 strong)
    /// </summary>
    /// <param name="pwd"></param>
    /// <returns></returns>
    public double scorePwd(string pwd)
    {
        var result = 0;
        if (string.IsNullOrEmpty(pwd))
            return result;

        // award every unique letter until 5 repetitions
        Hashtable chars = new();
        for (var i = 0; i <= pwd.Length - 1; i++)
        {
            chars[pwd[i]] = Utils.toInt(chars[pwd[i]]) + 1;
            result += (int)(5.0 / (double)chars[pwd[i]]);
        }

        // bonus points for mixing it up
        Hashtable vars = new()
        {
            {"digits",Regex.IsMatch(pwd, @"\d")},
            {"lower",Regex.IsMatch(pwd, "[a-z]")},
            {"upper",Regex.IsMatch(pwd, "[A-Z]")},
            {"other",Regex.IsMatch(pwd, @"\W")}
        };
        var ctr = 0;
        foreach (bool value in vars.Values)
        {
            if (value) ctr += 1;
        }
        result += (ctr - 1) * 10;

        // adjust for length
        result = (int)(Math.Log(pwd.Length) / Math.Log(8)) * result;

        return result;
    }

    /// <summary>
    /// generate a new MFA secret
    /// </summary>
    /// <returns></returns>
    internal string generateMFASecret()
    {
        return Base32Encoding.ToString(KeyGeneration.GenerateRandomKey());
    }

    public string generateMFAQRCode(string mfa_secret, string user = "user@company", string issuer = "osafw")
    {
        var uriString = new OtpUri(OtpType.Totp, mfa_secret, user, issuer).ToString();

        var IMG_SIZE = 5;
        return $"data:image/png;base64,{Convert.ToBase64String(PngByteQRCodeHelper.GetQRCode(uriString, QRCodeGenerator.ECCLevel.Q, IMG_SIZE))}";
    }

    /// <summary>
    /// check if code is valid against provided MFA secret
    /// </summary>
    /// <param name="mfa_secret"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public bool isValidMFACode(string mfa_secret, string code)
    {
        if (string.IsNullOrEmpty(mfa_secret))
            return false;

        var totp = new Totp(Base32Encoding.ToBytes(mfa_secret));
        // Generate the current TOTP value from the secret and compare it to the user's value.
        return totp.VerifyTotp(code, out _, new VerificationWindow(2, 2)); // use 1,1 for stricter time check
    }

    /// <summary>
    /// check if code is valid against user's MFA secret
    /// </summary>
    /// <param name="id">users.id</param>
    /// <param name="code"></param>
    /// <returns></returns>
    public bool isValidMFA(int id, string code)
    {
        var user = this.one(id);
        return isValidMFACode(user["mfa_secret"] ?? "", code);
    }

    /// <summary>
    /// check if code is a MFA recovery code, if yes - remove that code from user's recovery codes
    /// </summary>
    /// <param name="id"></param>
    /// <param name="code"></param>
    /// <returns>true if code is a recovery code</returns>
    public bool checkMFARecovery(int id, string code)
    {
        var result = false;
        var user = this.one(id);
        var recovery_codes = Utils.toStr(user["mfa_recovery"]).Split(' '); // space-separated hashed codes
        var new_recovery_codes = "";
        //split by space and check each code
        foreach (var recovery_code in recovery_codes)
        {
            if (checkPwd(code, recovery_code))
                result = true;
            else
                new_recovery_codes += recovery_code + " "; // not found codes - add to new list
        }

        if (result)
        {
            //if found - update user's recovery codes (as we removed matched one)
            var item = new Hashtable();
            item["mfa_recovery"] = new_recovery_codes.Trim();
            this.update(id, item);
        }

        return result;
    }
    #endregion

    #region Login/Session
    // fill the session and do all necessary things just user authenticated (and before redirect
    public void doLogin(int id)
    {
        fw.context.Session.Clear();
        fw.Session("XSS", Utils.getRandStr(16));

        reloadSession(id);

        fw.logActivity(FwLogTypes.ICODE_USERS_LOGIN, FwEntities.ICODE_USERS, id);
        // update login info
        Hashtable fields = new();
        fields["login_time"] = DB.NOW;
        this.update(id, fields);
    }

    public bool reloadSession(int id = 0)
    {
        if (id == 0)
            id = fw.userId;
        var user = one(id);

        fw.Session("user_id", Utils.toStr(id));
        fw.Session("login", user["email"]);
        fw.Session("access_level", user["access_level"]); //note, set as string
        fw.Session("lang", user["lang"]);
        fw.Session("ui_theme", user["ui_theme"]);
        fw.Session("ui_mode", user["ui_mode"]);
        // fw.SESSION("user", hU)

        var fname = user["fname"].Trim();
        var lname = user["lname"].Trim();
        if (!string.IsNullOrEmpty(fname) || !string.IsNullOrEmpty(lname))
            fw.Session("user_name", string.Join(" ", fname, lname).Trim());
        else
            fw.Session("user_name", user["email"]);

        var avatar_link = "";
        if (Utils.toInt(user["att_id"]) > 0)
            avatar_link = fw.model<Att>().getUrl(Utils.toInt(user["att_id"]), "s");
        fw.Session("user_avatar_link", avatar_link);

        return true;
    }
    #endregion

    #region Access Control
    /// <summary>
    /// return true if currently logged user has at least minimum requested access level
    /// </summary>
    /// <param name="min_acl">minimum required access level</param>
    /// <returns></returns>
    public bool isAccessLevel(int min_acl)
    {
        return fw.userAccessLevel >= min_acl;
    }

    /// <summary>
    /// if currently logged user has at least minimum requested access level. Throw AuthException if user's acl is not enough
    /// </summary>
    /// <param name="min_acl">minimum required access level</param>
    public void checkAccessLevel(int min_acl)
    {
        if (!isAccessLevel(min_acl))
        {
            throw new AuthException();
        }
    }

    /// <summary>
    /// return true if user is ReadOnly user
    /// </summary>
    /// <param name="id">optional, if not passed - currently logged user checked</param>
    /// <returns></returns>
    public bool isReadOnly(int id = -1)
    {
        var result = false;
        if (id == -1)
            id = fw.userId;

        if (id <= 0)
            return true; //if no user logged - readonly

        var user = one(id);
        if (Utils.toBool(user["is_readonly"]))
            result = true;

        return result;
    }

    /// <summary>
    /// check if logged user is readonly, if yes - throws AuthEception
    /// </summary>
    /// <param name="id">optional, if not passed - currently logged user checked</param>
    /// <exception cref="AuthException"></exception>
    public void checkReadOnly(int id = -1)
    {
        if (isReadOnly(id))
            throw new AuthException();
    }

    /// <summary>
    /// return true if roles support enabled
    /// </summary>
    /// <returns></returns>
    public bool isRoles()
    {
#if isRoles
        return true;
#else
        return false;
#endif
    }

    //
    /// <summary>
    /// get all RBAC info for the user/recource
    /// </summary>
    /// <param name="users_id"></param>
    /// <param name="resource_icode"></param>
    /// <returns>hashtable with permissions keys:
    ///     list => true if user has list permission
    ///     view => true if user has view permission
    ///     add => true if user has add permission
    ///     edit => true if user has edit permission
    ///     del => true if user has delete permission
    /// </returns>
    public Hashtable getRBAC(int? users_id = null, string resource_icode = null)
    {
#if isRoles
        var result = new Hashtable();

        int user_access_level;

        if (users_id == null)
        {
            users_id = fw.userId;
            user_access_level = fw.userAccessLevel;
        }
        else
        {
            var user = one(users_id);
            user_access_level = Utils.f2int(user["access_level"]);
        }

        if (user_access_level == ACL_SITEADMIN)
        {
            //siteadmin doesn't have roles - has access to everything - just set all permissions to true
            //logger(LogLevel.TRACE, "RBAC info (SITEADMIN):");
            return allPermissions();
        }


        if (string.IsNullOrEmpty(resource_icode))
            resource_icode = fw.route.controller;

        // read resource id
        var resource = fw.model<Resources>().oneByIcode(resource_icode);
        if (resource.Count == 0)
            return result; //if no resource defined - return empty result - basically access denied
        var resources_id = Utils.f2int(resource["id"]);

        //list all permissions for the resource and all user roles
        List<string> roles_ids;
        if (users_id == 0)
            //visitor
            roles_ids = [fw.model<Roles>().idVisitor().ToString()]; // visitor role for non-logged
        else
                roles_ids = fw.model<UsersRoles>().colLinkedIdsByMainId((int)users_id);

        // read all permissions for the resource and user's roles
        var rows = fw.model<RolesResourcesPermissions>().listByRolesResources(roles_ids, new int[] { resources_id });
        var permissions_ids = new List<string>();
        foreach (Hashtable row in rows)
        {
            permissions_ids.Add((string)row["permissions_id"]);
        }

        // now read all permissions by ids and set icodes to result
        var permissions_rows = fw.model<Permissions>().multi(permissions_ids);
        foreach (Hashtable row in permissions_rows)
        {
            result[row["icode"]] = true;
        }
#else
        var result = allPermissions(); //if no Roles support - always allow
#endif

        logger(LogLevel.TRACE, "RBAC info:", result);
        return result;
    }

    /// <summary>
    /// return all allowed permissions as { permissions.icode => true }
    /// </summary>
    /// <returns></returns>
    public Hashtable allPermissions()
    {
        var result = new Hashtable();
#if isRoles
        var permissions = fw.model<Permissions>().list();
        foreach (Hashtable permission in permissions)
        {
            result[permission["icode"]] = true;
        }
#else
        //if no Roles support - always allow all
        var icodes = Utils.qw("list view add edit del");
        foreach (var icode in icodes)
        {
            result[icode] = true;
        }
#endif
        return result;
    }

    /// <summary>
    /// shortcut for isAccessByRolesResourceAction with current user/controller
    /// </summary>
    /// <param name="resource_action"></param>
    /// <param name="resource_action_more"></param>
    /// <returns></returns>
    public bool isAccessByRolesAction(string resource_action, string resource_action_more = "")
    {
        return isAccessByRolesResourceAction(fw.userId, fw.route.controller, resource_action, resource_action_more);
    }

    /// <summary>
    /// check if currently logged user roles has access to controller/action
    /// </summary>
    /// <param name="users_id">usually currently logged user - fw.userId</param>
    /// <param name="resource_icode">resource code like controller name 'AdminUsers'</param>
    /// <param name="resource_action">resource action like controller's action 'Index' or '' </param>
    /// <param name="resource_action_more">optional additional action string, usually route.action_more to help distinguish sub-actions</param>
    /// <returns></returns>
    public bool isAccessByRolesResourceAction(int users_id, string resource_icode, string resource_action, string resource_action_more = "", Hashtable access_actions_to_permissions = null)
    {

#if isRoles
        // determine permission by resource action
        var permission_icode = fw.model<Permissions>().mapActionToPermission(resource_action, resource_action_more);

        if (access_actions_to_permissions != null)
        {
            //check if we have controller's permission's override for the action
            if (access_actions_to_permissions.ContainsKey(permission_icode))
                permission_icode = (string)access_actions_to_permissions[permission_icode];
        }

        var result = isAccessByRolesResourcePermission(users_id, resource_icode, permission_icode);
        if (!result)
            logger(LogLevel.DEBUG, "Access by Roles denied", new Hashtable {
                {"resource_icode", resource_icode },
                {"resource_action", resource_action },
                {"resource_action_more", resource_action_more },
                {"permission_icode", permission_icode },
                {"access_actions_to_permissions", access_actions_to_permissions },
            });
#else
        var result = true; //if no Roles support - always allow
#endif

        return result;
    }

    /// <summary>
    /// check if currently logged user roles has access to resource with specific permission
    /// </summary>
    /// <param name="users_id"></param>
    /// <param name="resource_icode"></param>
    /// <param name="permission_icode"></param>
    /// <returns></returns>
    public bool isAccessByRolesResourcePermission(int users_id, string resource_icode, string permission_icode)
    {
#if isRoles
        // read resource id
        var resource = fw.model<Resources>().oneByIcode(resource_icode);
        if (resource.Count == 0)
            return false; //if no resource defined - access denied
        var resources_id = Utils.f2int(resource["id"]);

        var permission = fw.model<Permissions>().oneByIcode(permission_icode);
        if (permission.Count == 0)
            return false; //if no permission defined - access denied
        var permissions_id = Utils.f2int(permission["id"]);

        // read all roles for user
        List<string> roles_ids;
        if (users_id == 0)
            roles_ids = [fw.model<Roles>().idVisitor().ToString()]; // visitor role for non-logged
        else
        {
            var user = one(users_id);
            if (Utils.f2int(user["access_level"]) == ACL_SITEADMIN)
            {
                //siteadmin doesn't have roles - has access to everything
                return true;
            }
            else
            {
                roles_ids = fw.model<UsersRoles>().colLinkedIdsByMainId(users_id); // logged user roles
            }
        }


        // check if any of user's roles has access to resource/permission
        var result = fw.model<RolesResourcesPermissions>().isExistsByResourcePermissionRoles(resources_id, permissions_id, roles_ids);
        if (!result)
            logger(LogLevel.DEBUG, "Access by Roles denied", DB.h("resource_icode", resource_icode, "permission_icode", permission_icode));
#else
        var result = true; //if no Roles support - always allow
#endif
        return result;
    }

    //shortcut to avoid calling UsersRoles directly
    public ArrayList listLinkedRoles(int users_id)
    {
#if isRoles
        return fw.model<UsersRoles>().listLinkedByMainId(users_id);
#else
        return new ArrayList();
#endif
    }

    //shortcut to avoid calling UsersRoles directly
    public void updateLinkedRoles(int users_id, Hashtable linked_keys)
    {
#if isRoles
        fw.model<UsersRoles>().updateJunctionByMainId(users_id, linked_keys);
#endif
    }

    #endregion

    #region Permanent Login Cookies
    public string createPermCookie(int id)
    {
        string cookieId = Utils.getRandStr(64);
        string hashed = Utils.sha256(cookieId);
        var fields = DB.h("cookie_id", hashed, "users_id", id);
        db.updateOrInsert(table_users_cookies, fields, DB.h("users_id", id));

        Utils.createCookie(fw, PERM_COOKIE_NAME, cookieId, 60 * 60 * 24 * PERM_COOKIE_DAYS);

        return cookieId;
    }

    public bool checkPermanentLogin()
    {
        var cookieId = Utils.getCookie(fw, PERM_COOKIE_NAME);
        if (!string.IsNullOrEmpty(cookieId))
        {
            var hashed = Utils.sha256(cookieId);
            DBRow row = db.row(table_users_cookies, DB.h("cookie_id", hashed));
            if (row.Count > 0)
            {
                doLogin(Utils.toInt(row["users_id"]));
                return true;
            }
            else
            {
                Utils.deleteCookie(fw, PERM_COOKIE_NAME);
            }
        }
        return false;
    }

    public void removePermCookie(int id)
    {
        Utils.deleteCookie(fw, PERM_COOKIE_NAME);
        db.del(table_users_cookies, DB.h("users_id", id));
        db.del(table_users_cookies, DB.h("add_time", db.opLE(DateTime.Now.AddYears(-1)))); // also cleanup old records (i.e. force re-login after a year)
    }
    #endregion

    public void loadMenuItems()
    {
        ArrayList menu_items = (ArrayList)FwCache.getValue("menu_items");

        if (menu_items == null)
        {
            // read main menu items for sidebar
            menu_items = db.array(table_menu_items, DB.h("status", STATUS_ACTIVE), "iname");
            FwCache.setValue("menu_items", menu_items);
        }

        // only Menu items user can see per ACL
        var users_acl = fw.userAccessLevel;
        ArrayList result = new();
        foreach (Hashtable item in menu_items)
        {
            if (Utils.toInt(item["access_level"]) <= users_acl)
                result.Add(item);
        }

        fw.G["menu_items"] = result;
    }
}