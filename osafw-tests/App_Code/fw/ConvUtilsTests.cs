using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using osafw;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace osafw.Tests
{
    [TestClass()]
    public class ConvUtilsTests
    {
        [TestMethod()]
        public void parsePagePdfTest()
        {
            IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("D:\\engineeredit\\osafw-asp.net-core\\osafw-app\\appsettings.json").Build();
            var context = new DefaultHttpContext();
            var fw = new FW(context, configuration);
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
            
            ConvUtils.parsePagePdf(fw, "./", "testing.tpl", ps, "./out.pdf", new Hashtable());
            System.IO.File.Delete("testing.tpl");
            Assert.IsTrue(System.IO.File.Exists("./out.pdf"));
        }
        public void html2pdfTest()
        {
            throw new NotImplementedException();
        }

        [TestMethod()]
        public void parsePageDocTest()
        {
            throw new NotImplementedException();
        }

        [TestMethod()]
        public void html2xlsTest()
        {
            throw new NotImplementedException();
        }

        [TestMethod()]
        public void parsePageExcelTest()
        {
            throw new NotImplementedException();
        }

        [TestMethod()]
        public void parsePageExcelSimpleTest()
        {
            throw new NotImplementedException();
        }
    }
}