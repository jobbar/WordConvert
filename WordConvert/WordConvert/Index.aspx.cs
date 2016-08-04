﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using FyDB;
using iTextSharp.text;
using iTextSharp.text.pdf;
using NPOI.HPSF;
using NPOI.HSSF.UserModel;

namespace WordConvert
{
    public partial class Index : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        //转换成html按钮
        protected void Button2_Click(object sender, EventArgs e)
        {
            //创建临时文件，避开浏览器不兼容问题
            string fname = Server.MapPath("~/wordInfo/") + Guid.NewGuid().ToString() + ".doc";
            this.FileUpload2.SaveAs(fname);

            GetPathByDocToHTML(fname);
        }

        #region wordFormToHtml

        /// <summary>
        /// word转换html
        /// </summary>
        /// <param name="strFile">全路径</param>
        /// <returns></returns>
        private string GetPathByDocToHTML(string strFile)
        {
            if (string.IsNullOrEmpty(strFile))
            {
                return "0"; //没有文件
            }

            Microsoft.Office.Interop.Word.ApplicationClass word = new Microsoft.Office.Interop.Word.ApplicationClass();
            Type wordType = word.GetType();
            Microsoft.Office.Interop.Word.Documents docs = word.Documents;

            // 打开文件  
            Type docsType = docs.GetType();

            object fileName = strFile;

            Microsoft.Office.Interop.Word.Document doc =
                (Microsoft.Office.Interop.Word.Document)docsType.InvokeMember("Open",
                    System.Reflection.BindingFlags.InvokeMethod, null, docs, new Object[] { fileName, true, true });

            // 转换格式，另存为html  
            Type docType = doc.GetType();
            //给文件重新起名
            string filename = System.DateTime.Now.Year.ToString() + System.DateTime.Now.Month.ToString() +
                              System.DateTime.Now.Day.ToString() +
                              System.DateTime.Now.Hour.ToString() + System.DateTime.Now.Minute.ToString() +
                              System.DateTime.Now.Second.ToString();

            string strFileFolder = "~/html/";
            DateTime dt = DateTime.Now;
            //以yyyymmdd形式生成子文件夹名
            string strFileSubFolder = dt.Year.ToString();
            strFileSubFolder += (dt.Month < 10) ? ("0" + dt.Month.ToString()) : dt.Month.ToString();
            strFileSubFolder += (dt.Day < 10) ? ("0" + dt.Day.ToString()) : dt.Day.ToString();
            string strFilePath = strFileFolder + strFileSubFolder + "/";
            // 判断指定目录下是否存在文件夹，如果不存在，则创建 
            if (!Directory.Exists(Server.MapPath(strFilePath)))
            {
                // 创建up文件夹 
                Directory.CreateDirectory(Server.MapPath(strFilePath));
            }

            //被转换的html文档保存的位置 
            // HttpContext.Current.Server.MapPath("html" + strFileSubFolder + filename + ".html")
            string ConfigPath = Server.MapPath(strFilePath + filename + ".html");
            object saveFileName = ConfigPath;

            /*下面是Microsoft Word 9 Object Library的写法，如果是10，可能写成： 
              * docType.InvokeMember("SaveAs", System.Reflection.BindingFlags.InvokeMethod, 
              * null, doc, new object[]{saveFileName, Word.WdSaveFormat.wdFormatFilteredHTML}); 
              * 其它格式： 
              * wdFormatHTML 
              * wdFormatDocument 
              * wdFormatDOSText 
              * wdFormatDOSTextLineBreaks 
              * wdFormatEncodedText 
              * wdFormatRTF 
              * wdFormatTemplate 
              * wdFormatText 
              * wdFormatTextLineBreaks 
              * wdFormatUnicodeText 
            */
            docType.InvokeMember("SaveAs", System.Reflection.BindingFlags.InvokeMethod,
                null, doc, new object[] { saveFileName, Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatFilteredHTML });

            //docType.InvokeMember("SaveAs", System.Reflection.BindingFlags.InvokeMethod,
            //  null, doc, new object[] { saveFileName, Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatFilteredHTML }); 

            //关闭文档  
            docType.InvokeMember("Close", System.Reflection.BindingFlags.InvokeMethod,
                null, doc, new object[] { null, null, null });

            // 退出 Word  
            wordType.InvokeMember("Quit", System.Reflection.BindingFlags.InvokeMethod, null, word, null);
            //转到新生成的页面  
            //return ("/" + filename + ".html");

            //转化HTML页面统一编码格式
            TransHTMLEncoding(ConfigPath);

            File.Delete(strFile); //删除临时保存的文件

            return (strFilePath + filename + ".html");
        }

        /// <summary>
        /// 编码转换
        /// </summary>
        /// <param name="strFilePath"></param>
        private void TransHTMLEncoding(string strFilePath)
        {
            try
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(strFilePath, Encoding.GetEncoding(0));
                string html = sr.ReadToEnd();
                sr.Close();
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<meta[^>]*>",
                    "<meta http-equiv=Content-Type content='text/html; charset=gb2312'>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                System.IO.StreamWriter sw = new System.IO.StreamWriter(strFilePath, false, Encoding.Default);

                sw.Write(html);
                sw.Close();
            }
            catch (Exception ex)
            {
                Page.RegisterStartupScript("alt", "<script>alert('" + ex.Message + "')</script>");
            }
        }

        #endregion

        #region DataTableFormToExcel

        /// <summary>
        /// 转换为excel
        /// </summary>
        /// <param name="tbName"></param>
        public void FormToExcel(string tbName, DataTable tblDatas)
        {
            NPOI.HSSF.UserModel.HSSFWorkbook book = new NPOI.HSSF.UserModel.HSSFWorkbook();
            var sheetReportResult = book.CreateSheet(tbName);

            for (int i = 0; i < tblDatas.Columns.Count; i++)//输出标题
            {
                if (i == 0)
                {
                    //产生第一个要用CreateRow 
                    sheetReportResult.CreateRow(0).CreateCell(i).SetCellValue(tblDatas.Columns[i].ColumnName);
                }
                else
                {
                    //之后的用GetRow 取得在CreateCell
                    sheetReportResult.GetRow(0).CreateCell(i).SetCellValue(tblDatas.Columns[i].ColumnName);
                }
            }

            //循环内容
            for (int i = 0; i < tblDatas.Rows.Count; i++)
            {
                sheetReportResult.CreateRow(i+1).CreateCell(0).SetCellValue(tblDatas.Rows[i][0].ToString());
                for (int j = 0; j < tblDatas.Columns.Count; j++)
                {
                    sheetReportResult.GetRow(i + 1).CreateCell(j).SetCellValue(tblDatas.Rows[i][j].ToString());
                }
            }

            sheetReportResult.SetColumnWidth(0, 20 * 256);
            sheetReportResult.SetColumnWidth(1, 40 * 256);
            sheetReportResult.SetColumnWidth(2, 40 * 256);

            // 写入到客户端  
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            book.Write(ms);
            Response.AddHeader("Content-Disposition",
                string.Format("attachment; filename={0}.xls", DateTime.Now.ToString("yyyyMMddHHmmssfff")));
            Response.BinaryWrite(ms.ToArray());
            book = null;
            ms.Close();
            ms.Dispose();
        }

        #endregion

        #region DataTableFormToWord

        public void ExportDataGridViewToWord(DataTable srcDgv)
        {
            if (srcDgv.Rows.Count == 0)
            {
                Page.RegisterStartupScript("alt", "<script>alert('没有数据可供导出!')</script>");
                return;
            }
            else
            {
                Object none = System.Reflection.Missing.Value;
                Microsoft.Office.Interop.Word.Application wordApp = new Microsoft.Office.Interop.Word.Application();
                Microsoft.Office.Interop.Word.Document document = wordApp.Documents.Add(ref none, ref none, ref none, ref none);
                //建立表格
                Microsoft.Office.Interop.Word.Table table = document.Tables.Add(document.Paragraphs.Last.Range, srcDgv.Rows.Count + 1, srcDgv.Columns.Count, ref none, ref none);
                try
                {
                    for (int i = 0; i < srcDgv.Columns.Count; i++)//输出标题
                    {
                        table.Cell(1, i + 1).Range.Text = srcDgv.Columns[i].ColumnName;
                    }
                    //输出控件中的记录
                    for (int i = 0; i < srcDgv.Rows.Count; i++)
                    {
                        for (int j = 0; j < srcDgv.Columns.Count; j++)
                        {
                            table.Cell(i + 2, j + 1).Range.Text = srcDgv.Rows[i][j].ToString();
                        }
                    }
                    table.Borders.OutsideLineStyle = Microsoft.Office.Interop.Word.WdLineStyle.wdLineStyleSingle;
                    table.Borders.InsideLineStyle = Microsoft.Office.Interop.Word.WdLineStyle.wdLineStyleSingle;
                    string newFile = "c://" + DateTime.Now.ToString("yyyyMMddHHmmssss") + ".doc";
                    document.SaveAs(newFile, ref none, ref none, ref none, ref none, ref none, ref none, ref none, ref none, ref none, ref none, ref none, ref none, ref none, ref none, ref none);

                    Page.RegisterStartupScript("alt", "<script>alert('数据成功导出!')</script>");
                }
                catch (Exception e)
                {
                    Page.RegisterStartupScript("alt", "<script>alert('" + e.Message + "')</script>");
                }
            }
        }
        #endregion

        #region DataTableFormToPdf

        public void FormToPdf(DataTable srcDgv)
        {
            Document document = new Document();
            string newFile = "c://" + DateTime.Now.ToString("yyyyMMddHHmmssss") + ".pdf";
            PdfWriter.GetInstance(document, new FileStream(newFile, FileMode.Create));
            document.Open();
            BaseFont bfChinese = BaseFont.CreateFont("C://WINDOWS//Fonts//simsun.ttc,1", BaseFont.IDENTITY_H,
                BaseFont.NOT_EMBEDDED);
            iTextSharp.text.Font fontChinese = new iTextSharp.text.Font(bfChinese, 12,
                iTextSharp.text.Font.NORMAL, new iTextSharp.text.Color(0, 0, 0));

            StringBuilder sbBuilder = new StringBuilder();
            for (int i = 0; i < srcDgv.Columns.Count; i++)//输出标题
            {
                sbBuilder.Append(srcDgv.Columns[i].ColumnName + "   \t");
            }
            sbBuilder.Append("\n");
            //输出控件中的记录
            for (int i = 0; i < srcDgv.Rows.Count; i++)
            {
                for (int j = 0; j < srcDgv.Columns.Count; j++)
                {
                    sbBuilder.Append(srcDgv.Rows[i][j].ToString() + "   \t");
                }
                sbBuilder.Append("\n");
            }
            //导出文本的内容：
            document.Add(new Paragraph(sbBuilder.ToString(), fontChinese));
            //导出图片：
            //iTextSharp.text.Image jpeg = iTextSharp.text.Image.GetInstance(Path.GetFullPath("1.jpg"));
            //document.Add(jpeg);

            //注意一定要关闭，否则PDF中的内容将得不到保存

            document.Close();
            Page.RegisterStartupScript("alt", "<script>alert('数据成功导出!')</script>");
        }
        #endregion
        public static DataTable ConvertDataReaderToDataTable(SqlDataReader reader)
        {
            try
            {
                DataTable objDataTable = new DataTable();
                int intFieldCount = reader.FieldCount;
                for (int intCounter = 0; intCounter < intFieldCount; ++intCounter)
                {
                    objDataTable.Columns.Add(reader.GetName(intCounter), reader.GetFieldType(intCounter));
                }
                objDataTable.BeginLoadData();

                object[] objValues = new object[intFieldCount];
                while (reader.Read())
                {
                    reader.GetValues(objValues);
                    objDataTable.LoadDataRow(objValues, true);
                }
                reader.Close();
                objDataTable.EndLoadData();

                return objDataTable;

            }
            catch (Exception ex)
            {
                throw new Exception("转换出错!", ex);
            }

        }

        //导出按钮
        protected void Button3_Click(object sender, EventArgs e)
        {
            if (this.tbName.Text != "")
            {
                string sql = string.Format("select * from {0}", this.tbName.Text);
                SqlDataReader reader = SqlHelper.ExecuteReader(SqlHelper.ConnectionStringLocalTransaction, CommandType.Text,
                         sql, null);
                DataTable tblDatas = ConvertDataReaderToDataTable(reader);
                switch (this.DropDownList1.SelectedIndex)
                {
                    case 0: //word
                        ExportDataGridViewToWord(tblDatas);
                        break;
                    case 1: //excel
                        FormToExcel(this.tbName.Text, tblDatas);
                        break;
                    case 2: //pdf
                        FormToPdf(tblDatas);
                        break;
                }
            }
            else
            {
                Page.RegisterStartupScript("alt", "<script>alert('请填写要转换的表名!')</script>");
            }

        }
    }
}
