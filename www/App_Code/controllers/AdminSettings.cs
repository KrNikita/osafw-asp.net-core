﻿// Site Settings Admin  controller
//
// Part of ASP.NET osa framework  www.osalabs.com/osafw/asp.net
// (c) 2009-2021 Oleg Savchuk www.osalabs.com

using System;
using System.Collections;

namespace osafw
{
    public class AdminSettingsController : FwAdminController
    {
        public static new int access_level = Users.ACL_SITEADMIN;

        protected Settings model;

        public override void init(FW fw)
        {
            base.init(fw);
            model = fw.model<Settings>();
            model0 = model;

            base_url = "/Admin/Settings";
            required_fields = "ivalue";
            save_fields = "ivalue";
            save_fields_checkboxes = "";

            search_fields = "icode iname ivalue";
            list_sortdef = "iname asc";
            list_sortmap = Utils.qh("id|id iname|iname upd_time|upd_time");
        }

        public override void setListSearch()
        {
            this.list_where = " 1=1 ";
            base.setListSearch();

            if (!string.IsNullOrEmpty((string)list_filter["s"]))
                list_where += " and icat=" + db.qi(list_filter["s"]);
        }

        public override Hashtable ShowFormAction(string form_id = "")
        {
            // set new form defaults here if any
            // Me.form_new_defaults = New Hashtable
            // item("field")="default value"
            Hashtable ps = base.ShowFormAction(form_id);

            Hashtable item = (Hashtable)ps["i"];
            // TODO - multi values for select, checkboxes, radio
            // ps("select_options_parent_id") = FormUtils.select_options_db(db.array("select id, iname from " & model.table_name & " where parent_id=0 and status=0 order by iname"), item("parent_id"))
            // ps("multi_datarow") = fw.model(Of DemoDicts).get_multi_list(item("dict_link_multi"))

            return ps;
        }

        public override Hashtable SaveAction(string form_id = "")
        {
            if (this.save_fields == null)
                throw new Exception("No fields to save defined, define in save_fields ");

            Hashtable item = reqh("item");
            int id = Utils.f2int(form_id);
            var success = true;
            var is_new = (id == 0);
            var location = "";

            try
            {
                Validate(id, item);
                // load old record if necessary
                // Dim item_old As Hashtable = model.one(id)

                Hashtable itemdb = FormUtils.filter(item, this.save_fields);
                // TODO - checkboxes
                // FormUtils.form2dbhash_checkboxes(itemdb, item, save_fields_checkboxes)
                // itemdb("dict_link_multi") = FormUtils.multi2ids(reqh("dict_link_multi"))

                // only update, no add new settings
                model.update(id, itemdb);
                fw.flash("record_updated", 1);

                // custom code:
                // reset cache
                FwCache.remove("main_menu");

                location = base_url;
            }
            catch (ApplicationException ex)
            {
                success = false;
                this.setFormError(ex);
            }

            return this.afterSave(success, id, is_new, "ShowForm", location);
        }

        public override void Validate(int id, Hashtable item)
        {
            bool result = this.validateRequired(item, this.required_fields);

            if (id == 0)
                throw new ApplicationException("Wrong Settings ID");

            this.validateCheckResult();
        }

        public override Hashtable DeleteAction(string form_id)
        {
            throw new ApplicationException("Site Settings cannot be deleted");
        }
    }
}