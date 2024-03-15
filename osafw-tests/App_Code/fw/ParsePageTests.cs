﻿using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using osafw;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osafw.Tests
{
    [TestClass()]
    public class ParsePageTests
    {
        [TestMethod()]
        public void clear_cacheTest()
        {
            string tpl = "<~AAA><br/><~BBB><br/><~CCC><br/><~DDD><br/><~arr repeat inline><~AAA><br/><~BBB><br/><~CCC><br/><~DDD><br/></~arr>";
            System.IO.File.WriteAllText("testing.tpl", tpl);

            Hashtable h1 = new();
            h1["AAA"] = 1;
            h1["BBB"] = 2;
            h1["CCC"] = 3;
            h1["DDD"] = 4;

            ArrayList arr = new();
            arr.Add(h1);
            arr.Add(h1);
            arr.Add(h1);
            Hashtable ps = new() { { "arr", arr } };
            ps["AAA"] = 1;
            ps["BBB"] = 2;
            ps["CCC"] = 3;
            ps["DDD"] = 4;

            ParsePage parsePage = new ParsePage(null);
            string r = parsePage.parse_page("./", "testing.tpl", ps);
            var resultStr = "1<br/>2<br/>3<br/>4<br/>1<br/>2<br/>3<br/>4<br/>1<br/>2<br/>3<br/>4<br/>1<br/>2<br/>3<br/>4<br/>";
            Assert.AreEqual(resultStr, r);

            // just check that clear_cache are not fail, since FILE_CACHE and LANG_CACHE are private members
            parsePage.clear_cache();

            System.IO.File.Delete("testing.tpl");
        }

        [TestMethod()]
        public void tag_tplpathTest()
        {
            ParsePage parsePage = new ParsePage(null);
            var r = parsePage.tag_tplpath("arr", "testing.tpl");

            Assert.AreEqual("arr.html", r);
        }

        // TODO to test this need changes in the framework
        //[TestMethod()]
        //public void langMapTest()
        //{
        //    throw new NotImplementedException();
        //}

        [TestMethod()]
        public void parse_pageTest()
        {
            string tpl = "<~AAA><br/><~BBB><br/><~CCC><br/><~DDD><br/><~arr repeat inline><~AAA><br/><~BBB><br/><~CCC><br/><~DDD><br/></~arr>";
            System.IO.File.WriteAllText("testing.tpl", tpl);

            Hashtable h1 = new();
            h1["AAA"] = 1;
            h1["BBB"] = 2;
            h1["CCC"] = 3;
            h1["DDD"] = 4;

            ArrayList arr = new();
            arr.Add(h1);
            arr.Add(h1);
            arr.Add(h1);
            Hashtable ps = new() { { "arr", arr } };
            ps["AAA"] = 1;
            ps["BBB"] = 2;
            ps["CCC"] = 3;
            ps["DDD"] = 4;

            string r = new ParsePage(null).parse_page("./", "testing.tpl", ps);
            var resultStr = "1<br/>2<br/>3<br/>4<br/>1<br/>2<br/>3<br/>4<br/>1<br/>2<br/>3<br/>4<br/>1<br/>2<br/>3<br/>4<br/>";
            Assert.AreEqual(resultStr, r);
            System.IO.File.Delete("testing.tpl");
        }

        [TestMethod()]
        public void parse_jsonTest()
        {
            Hashtable h1 = new();
            h1["AAA"] = 1;
            h1["BBB"] = 2;
            h1["CCC"] = 3;
            h1["DDD"] = 4;

            var p = new ParsePage(null);
            string r = p.parse_json(h1);

            Assert.AreEqual(0, r.IndexOf("{"));
            Assert.IsTrue(r.IndexOf("\"AAA\":1") >= 0);
            Assert.IsTrue(r.IndexOf("\"BBB\":2") >= 0);
            Assert.IsTrue(r.IndexOf("\"CCC\":3") >= 0);
            Assert.IsTrue(r.IndexOf("\"DDD\":4") >= 0);

            bool isException = false;
            try
            {
                _ = p.parse_json(null);
            }
            catch (NullReferenceException)
            {
                isException = true;
            }
            Assert.IsTrue(isException);

        }

        [TestMethod()]
        public void parse_stringTest()
        {
            string tpl = "<~AAA><br/><~BBB><br/><~CCC><br/><~DDD><br/>";
            Hashtable h1 = new();
            h1["AAA"] = 1;
            h1["BBB"] = 2;
            h1["CCC"] = 3;
            h1["DDD"] = 4;

            string r = new ParsePage(null).parse_string(tpl, h1);
            Assert.AreEqual("1<br/>2<br/>3<br/>4<br/>", r);
        }

        [TestMethod()]
        public void parse_string_repeatTest()
        {
            string tpl = "<~arr repeat inline><~AAA><br/><~BBB><br/><~CCC><br/><~DDD><br/></~arr>";
            Hashtable h1 = new();
            h1["AAA"] = 1;
            h1["BBB"] = 2;
            h1["CCC"] = 3;
            h1["DDD"] = 4;
            ArrayList arr = new();
            arr.Add(h1);
            arr.Add(h1);
            arr.Add(h1);
            Hashtable ps = new() { { "arr", arr } };

            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("1<br/>2<br/>3<br/>4<br/>1<br/>2<br/>3<br/>4<br/>1<br/>2<br/>3<br/>4<br/>", r);
        }

        [TestMethod()]
        public void parse_string_ifTest()
        {
            string tpl = "<~if_block if=\"AAA\" inline>Text</~if_block>";
            Hashtable ps = new();

            ps["AAA"] = 1;
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);

            ps["AAA"] = true;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);

            ps["AAA"] = new Hashtable() { { "AAA", 1} };
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);

            ps["AAA"] = 0;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);

            ps["AAA"] = null;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);

            ps["AAA"] = false;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
        }

        [TestMethod()]
        public void parse_string_unlessTest()
        {
            string tpl = "<~if_block unless=\"AAA\" inline>Text</~if_block>";
            Hashtable ps = new();
            
            ps["AAA"] = 0;
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);
            ps["AAA"] = false;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);
            ps["AAA"] = null;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);

            ps["AAA"] = 1;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
            ps["AAA"] = true;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
            ps["AAA"] = new Hashtable() { { "AAA", 1 } };
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
        }

        [TestMethod()]
        public void parse_string_ifqeTest()
        {
            string tpl = "<~if_block ifeq=\"AAA\" vvalue=\"value\" inline>Text</~if_block>";
            Hashtable ps = new();
            
            ps["AAA"] = "test";
            ps["value"] = "test";
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);
            ps["AAA"] = 123;
            ps["value"] = 123;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);
            ps["AAA"] = true;
            ps["value"] = true;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);


            ps["AAA"] = "test1";
            ps["value"] = "test";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
            ps["AAA"] = 1234;
            ps["value"] = 123;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
            ps["AAA"] = true;
            ps["value"] = false;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
        }

        [TestMethod()]
        public void parse_string_ifneTest()
        {
            string tpl = "<~if_block ifne=\"AAA\" vvalue=\"value\" inline>Text</~if_block>";
            Hashtable ps = new();

            ps["AAA"] = "test1";
            ps["value"] = "test";
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);
            ps["AAA"] = 1234;
            ps["value"] = 123;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);
            ps["AAA"] = true;
            ps["value"] = false;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);


            ps["AAA"] = "test";
            ps["value"] = "test";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
            ps["AAA"] = 123;
            ps["value"] = 123;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
            ps["AAA"] = false;
            ps["value"] = false;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
        }

        [TestMethod()]
        public void parse_string_ifgtTest()
        {
            string tpl = "<~if_block ifgt=\"AAA\" vvalue=\"value\" inline>Text</~if_block>";
            Hashtable ps = new();

            ps["AAA"] = 100;
            ps["value"] = 10;
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);


            ps["AAA"] = 10;
            ps["value"] = 100;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
            ps["AAA"] = 100;
            ps["value"] = 100;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
        }

        [TestMethod()]
        public void parse_string_ifgeTest()
        {
            string tpl = "<~if_block ifge=\"AAA\" vvalue=\"value\" inline>Text</~if_block>";
            Hashtable ps = new();

            ps["AAA"] = 100;
            ps["value"] = 10;
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);

            ps["AAA"] = 100;
            ps["value"] = 100;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);


            ps["AAA"] = 10;
            ps["value"] = 100;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
        }

        [TestMethod()]
        public void parse_string_ifltTest()
        {
            string tpl = "<~if_block iflt=\"AAA\" vvalue=\"value\" inline>Text</~if_block>";
            Hashtable ps = new();

            ps["AAA"] = 10;
            ps["value"] = 100;
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);


            ps["AAA"] = 100;
            ps["value"] = 10;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
            ps["AAA"] = 100;
            ps["value"] = 100;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
        }

        [TestMethod()]
        public void parse_string_ifleTest()
        {
            string tpl = "<~if_block ifle=\"AAA\" vvalue=\"value\" inline>Text</~if_block>";
            Hashtable ps = new();

            ps["AAA"] = 10;
            ps["value"] = 100;
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);

            ps["AAA"] = 100;
            ps["value"] = 100;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Text", r);


            ps["AAA"] = 100;
            ps["value"] = 10;
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("", r);
        }

        [TestMethod()]
        public void parse_string_selectTest()
        {
            string tpl = "<select name = \"item[fruit]\">" +
                "<option value=\"\">- select a fruit -</option>" +
                "<~fruits_select select=\"fruit\">" +
                "</select>";
            string tpl_result = "<select name = \"item[fruit]\">" +
                "<option value=\"\">- select a fruit -</option>" +
                "<option value=\"1\">Apple</option>\r\n" +
                "<option value=\"2\">Plum</option>\r\n" +
                "<option value=\"3\" selected>Banana</option>\r\n" +
                "</select>";
            Hashtable ps = new();
            ps["fruits_select"] = new ArrayList() {
                new Hashtable() { { "id", "1" }, { "iname", "Apple" } },
                new Hashtable() { { "id", "2" }, { "iname", "Plum" } },
                new Hashtable() { { "id", "3" }, { "iname", "Banana" } }
            };
            ps["fruit"] = "3";
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual(tpl_result, r);
        }


        //TODO To test this change to the framework needed
        //[TestMethod()]
        //public void parse_string_ratioTest()
        //{
        //    var parsePage = new ParsePage(null);
        //    //parsePage.precache_file("fcombo.sel");
        //    string tpl = "<~fcombo.sel radio=\"fradio\" name=\"item[fradio]\" delim=\"&nbsp;\">";
        //    string tpl_result = "<div class='form-check &nbsp;'><input class='form-check-input' type='radio' name=\"item[fradio]\" id=\"item[fradio]$0\" value=\"1\"><label class='form-check-label' for='item[fradio]$0'>Apple</label></div>" +
        //        "<div class='form-check &nbsp;'><input class='form-check-input' type='radio' name=\"item[fradio]\" id=\"item[fradio]$1\" value=\"2\"><label class='form-check-label' for='item[fradio]$1'>Plum</label></div>" +
        //        "<div class='form-check &nbsp;'><input class='form-check-input' type='radio' name=\"item[fradio]\" id=\"item[fradio]$2\" value=\"3\"><label class='form-check-label' for='item[fradio]$2'>Banana</label></div>";
        //    Hashtable ps = new();
        //    ps["fcombo"] = new ArrayList() {
        //        new Hashtable() { { "id", "1" }, { "iname", "Apple" } },
        //        new Hashtable() { { "id", "2" }, { "iname", "Plum" } },
        //        new Hashtable() { { "id", "3" }, { "iname", "Banana" } }
        //    };
        //    var r = parsePage.parse_string(tpl, ps);
        //    Assert.AreEqual(tpl_result, r);
        //}

        [TestMethod()]
        public void parse_string_htmlescapeTest()
        {
            string tpl = "<~AAA htmlescape>";
            Hashtable ps = new();
            ps["AAA"] = "<p>tag</p>";
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("&amp;lt;p&amp;gt;tag&amp;lt;/p&amp;gt;", r);

            tpl = "<~AAA>";
            ps["AAA"] = "<p>tag</p>";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("&lt;p&gt;tag&lt;/p&gt;", r);
        }

        [TestMethod()]
        public void parse_string_noescapeTest()
        {
            string tpl = "<~AAA noescape>";
            Hashtable ps = new();
            ps["AAA"] = "<p>tag</p>";
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("<p>tag</p>", r);

            tpl = "<~AAA>";
            ps["AAA"] = "<p>tag</p>";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("&lt;p&gt;tag&lt;/p&gt;", r);
        }

        [TestMethod()]
        public void parse_string_urlTest()
        {
            string tpl = "<~AAA url>";
            Hashtable ps = new();
            ps["AAA"] = "test.com";
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("http://test.com", r);

            tpl = "<~AAA>";
            ps["AAA"] = "test.com";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("test.com", r);
        }

        [TestMethod()]
        public void parse_string_number_formatTest()
        {
            string tpl = "<~AAA>";
            Hashtable ps = new();
            ps["AAA"] = "123456.789";
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("123456.789", r);

            tpl = "<~AAA number_format>";
            ps["AAA"] = "123456.789";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("123,456.79", r);

            tpl = "<~AAA number_format=\"1\" nfthousands=\"\">";
            ps["AAA"] = "123456.789";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("123456.8", r);
        }

        [TestMethod()]
        public void parse_string_dateTest()
        {
            DateTime d = DateTime.Now;
            string tpl = "<~AAA>";
            Hashtable ps = new();
            ps["AAA"] = d;
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual(d.ToString("M/dd/yyyy h:m:ss tt"), r);

            tpl = "<~AAA date>";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual(d.ToString("M/dd/yyyy"), r);

            tpl = "<~AAA date=\"short\">";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual(d.ToString("M/dd/yyyy HH:mm"), r);

            tpl = "<~AAA date=\"long\">";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual(d.ToString("M/dd/yyyy HH:mm:ss"), r);

            tpl = "<~AAA date=\"sql\">";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual(d.ToString("yyyy-MM-dd HH:mm:ss"), r);


            tpl = "<~AAA date=\"d M Y H:i\">";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual(d.ToString("d M Y H:i"), r);
        }


        [TestMethod()]
        public void parse_string_truncateTest()
        {
            string tpl = "<~AAA truncate>";
            Hashtable ps = new();
            ps["AAA"] = "test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test ";
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual(ps["AAA"].ToString().Substring(0, 80).Trim() + " test...", r);

            tpl = "<~AAA>";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual(ps["AAA"], r);
        }

        [TestMethod()]
        public void parse_string_strip_tagsTest()
        {
            string tpl = "<~AAA noescape strip_tags>";
            Hashtable ps = new();
            ps["AAA"] = "<p>tag</p>";
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("tag", r);

            tpl = "<~AAA noescape>";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual(ps["AAA"], r);
        }

        [TestMethod()]
        public void parse_string_trimTest()
        {
            string tpl = "<~AAA trim>";
            Hashtable ps = new();
            ps["AAA"] = " tag ";
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("tag", r);
        }

        [TestMethod()]
        public void parse_string_nl2brTest()
        {
            string tpl = "<~AAA nl2br>";
            Hashtable ps = new();
            ps["AAA"] = "tag\ntag2";
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("tag<br>tag2", r);
        }

        [TestMethod()]
        public void parse_string_lowerTest()
        {
            string tpl = "<~AAA lower>";
            Hashtable ps = new();
            ps["AAA"] = "TAG";
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("tag", r);
            Assert.AreNotEqual(ps["AAA"], r);
        }

        [TestMethod()]
        public void parse_string_upperTest()
        {
            string tpl = "<~AAA upper>";
            Hashtable ps = new();
            ps["AAA"] = "tag";
            string r  = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("TAG", r);
            Assert.AreNotEqual(ps["AAA"], r);
        }


        [TestMethod()]
        public void parse_string_capitalizeTest()
        {
            string tpl = "<~AAA capitalize>";
            Hashtable ps = new();
            ps["AAA"] = "test test1 test2";
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Test test1 test2", r);

            tpl = "<~AAA capitalize=\"all\">";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("Test Test1 Test2", r);

            Assert.AreNotEqual(ps["AAA"], r);
        }

        [TestMethod()]
        public void parse_string_defaultTest()
        {
            string tpl = "<~AAA default=\"default value\">";
            Hashtable ps = new();
            ps["AAA"] = "tag";
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("tag", r);

            ps["AAA"] = "";
            r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("default value", r);
        }


        [TestMethod()]
        public void parse_string_urlencodeTest()
        {
            string tpl = "<~AAA urlencode>";
            Hashtable ps = new();
            ps["AAA"] = "item[tag]=1&item[tag2]=2";
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.AreEqual("item%5btag%5d%3d1%26amp%3bitem%5btag2%5d%3d2", r);
        }


        [TestMethod()]
        public void parse_string_jsonTest()
        {
            string tpl = "<~AAA json>";
            Hashtable ps = new();
            Hashtable h1 = new();
            h1["AAA"] = 1;
            h1["BBB"] = 2;
            h1["CCC"] = 3;
            h1["DDD"] = 4;
            ps["AAA"] = h1;
            string r = new ParsePage(null).parse_string(tpl, ps);
            Assert.IsTrue(r.IndexOf("{") == 0);
            Assert.IsTrue(r.IndexOf("&quot;AAA&quot;:1") >= 0);
            Assert.IsTrue(r.IndexOf("&quot;BBB&quot;:2") >= 0);
            Assert.IsTrue(r.IndexOf("&quot;CCC&quot;:3") >= 0);
            Assert.IsTrue(r.IndexOf("&quot;DDD&quot;:4") >= 0);
        }

    }
}