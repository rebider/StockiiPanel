﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data;
using System.Windows.Forms;
using System.Collections;//在C#中使用ArrayList必须引用Collections类
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Xml.Serialization;

namespace StockiiPanel
{
    /// <summary>
    /// 公共接口类
    /// </summary>
    partial class Commons
    {
        /// <summary>
        /// 导出列表中数据到CSV中
        /// </summary>
        /// <param name="dt">要导出的DataTable</param>
        public static void ExportDataGridToCSV(DataTable dt)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = "*.csv";
            saveFileDialog.AddExtension = true;
            saveFileDialog.Filter = "csv files|*.csv";
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.CheckPathExists = true;
            saveFileDialog.FileName = "";

            saveFileDialog.ShowDialog();
            if (saveFileDialog.FileName != null) //打开保存文件对话框
            {
                string fileName = saveFileDialog.FileName;//文件名字
                if (fileName.Equals(""))
                    return;

                using (StreamWriter streamWriter = new StreamWriter(fileName, false, Encoding.Default))
                {
                    //Tabel header
                    //for (int i = 0; i < dt.Columns.Count; i++)
                    //{
                    //    streamWriter.Write(dt.Columns[i].ColumnName);
                    //    streamWriter.Write(",");
                    //}
                    //streamWriter.WriteLine("");
                    //Table body
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        for (int j = 0; j < dt.Columns.Count; j++)
                        {
                            if (j == 0)
                            {
                                streamWriter.Write("=\"" + dt.Rows[i][j] + "\"");
                            }
                            else
                            {
                                streamWriter.Write(dt.Rows[i][j]);
                            }

                            streamWriter.Write(",");
                        }
                        streamWriter.WriteLine("");
                    }
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }
        }

        /// <summary>
        /// Delete special symbol
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string DelQuota(string str)
        {
            string result = str;
            string[] strQuota = { "~", "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "`", ";", "'", ",", ".", "/", ":", "/,", "<", ">", "?" };
            for (int i = 0; i < strQuota.Length; i++)
            {
                if (result.IndexOf(strQuota[i]) > -1)
                    result = result.Replace(strQuota[i], "");
            }
            return result;
        }

        /// <summary>
        /// list转化成DataTable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="varlist"></param>
        /// <returns></returns>
        public static DataTable ToDataTable<T>(IEnumerable<T> varlist, string name)
        {
            DataTable dtReturn = new DataTable(name);
            // column names 
            PropertyInfo[] oProps = null;
            if (varlist == null)
                return dtReturn;
            foreach (T rec in varlist)
            {
                if (oProps == null)
                {
                    oProps = ((Type)rec.GetType()).GetProperties();
                    foreach (PropertyInfo pi in oProps)
                    {
                        Type colType = pi.PropertyType;
                        if ((colType.IsGenericType) && (colType.GetGenericTypeDefinition()
                             == typeof(Nullable<>)))
                        {
                            colType = colType.GetGenericArguments()[0];
                        }
                        dtReturn.Columns.Add(new DataColumn(pi.Name, colType));
                    }
                }
                DataRow dr = dtReturn.NewRow();
                foreach (PropertyInfo pi in oProps)
                {
                    dr[pi.Name] = pi.GetValue(rec, null) == null ? DBNull.Value : pi.GetValue
                    (rec, null);
                }
                dtReturn.Rows.Add(dr);
            }
            return dtReturn;
        }

        /// <summary>
        /// 判断是不是交易日
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static bool isTradeDay(DateTime date)
        {
            DateTime riqi = Convert.ToDateTime(date.ToShortDateString() + "T00:00:00" + date.ToString("zzz"));
            if (tradeDates.Contains(riqi))
            {
                return true;
            }
            return false;
        }

        public static DateTime findNearDate(DateTime curDate)
        {
            foreach (DateTime dt in tradeDates)
                if (dt >= curDate)
                    return dt;
            return curDate;
        }

        public static DateTime calcStartDate(String curDateStr, int delta, int type)
        {
            DateTimeFormatInfo dtfi = new CultureInfo("zh-CN", false).DateTimeFormat;
            DateTime startDate = DateTime.ParseExact(curDateStr, "yyyy-MM-ddThh:mm:sszzz", dtfi, DateTimeStyles.None);
            switch (type)
            {
                case 1:
                    int index = tradeDates.IndexOf(startDate) - delta + 1;
                    if (index < 0)
                        index = 0;
                    startDate = tradeDates[index];
                    break;
                case 2:
                    delta = 7 * (delta - 1);
                    delta += Convert.ToInt32(startDate.DayOfWeek.ToString("d"));
                    startDate = startDate.AddDays(-delta);
                    startDate = findNearDate(startDate);
                    break;
                case 3:
                    startDate = findNearDate(new DateTime(startDate.Year, startDate.Month, 1));
                    break;
            }
            return startDate;
        }

        /// <summary>
        /// 将两个DataTable纵向合并
        /// </summary>
        /// <param name="hostDt">主表</param>
        /// <param name="clientDt">拼接表</param>
        public static void AppendDataTable(DataTable hostDt, DataTable clientDt)
        {
            if (hostDt != null)
            {
                DataRow dr;

                for (int i = 0; i < clientDt.Columns.Count; i++)
                {
                    if (hostDt.Columns.Contains(clientDt.Columns[i].ColumnName))
                    {
                        Random ro = new Random();
                        hostDt.Columns.Add(new DataColumn(clientDt.Columns[i].ColumnName + ro.Next()));
                    }
                    else
                        hostDt.Columns.Add(new DataColumn(clientDt.Columns[i].ColumnName));

                    if (clientDt.Rows.Count > 0)
                        for (int j = 0; j < clientDt.Rows.Count; j++)
                        {
                            dr = hostDt.Rows[j];
                            dr[hostDt.Columns.Count - 1] = clientDt.Rows[j][i];
                            dr = null;
                        }
                }
            }
        }

        /// <summary>
        /// 拼接功能
        /// </summary>
        /// <param name="dataGridView">要拼接的</param>
        /// <param name="isSelect">是否选中行</param>
        public static DataTable Combine(DevComponents.DotNetBar.Controls.DataGridViewX dataGridView, DevComponents.DotNetBar.Controls.DataGridViewX combineGridView, bool isSelect)
        {
            DataTable tb1 = ((DataSet)dataGridView.DataSource).Tables[0];
            DataTable tb2 = new DataTable();//临时表
            tb2 = tb1.Clone();

            //生成临时表保存选中或全部列
            if (isSelect)
            {
                for (int r = dataGridView.SelectedRows.Count - 1; r >= 0; r--)
                {
                    DataRow dataRow = tb2.NewRow();
                    for (int c = 0; c < dataGridView.Columns.Count; c++)
                    {
                        dataRow[c] = dataGridView.SelectedRows[r].Cells[c].Value;
                    }
                    tb2.Rows.Add(dataRow);
                }
            }
            else
            {
                for (int r = 0; r < dataGridView.Rows.Count; r++)
                {
                    DataRow dataRow = tb2.NewRow();
                    for (int c = 0; c < dataGridView.Columns.Count; c++)
                    {
                        dataRow[c] = dataGridView.Rows[r].Cells[c].Value;
                    }
                    tb2.Rows.Add(dataRow);
                }
            }

            ////原有的表和选定的表中ID相同的项按列拼接
            //if (combineGridView.RowCount > 0)
            //{
            //    DataTable tb3 = (DataTable)combineGridView.DataSource;
            //    DataTable tb4 = new DataTable();//临时表
            //    tb4 = tb3.Copy();

            //    ArrayList host = new ArrayList();
            //    ArrayList client = new ArrayList();
            //    for (int i = tb4.Rows.Count - 1; i >= 0; i--)
            //    {
            //        DataRow dr = tb4.Rows[i];
            //        for (int j = tb2.Rows.Count - 1; j >= 0; j--)
            //        {
            //            DataRow re = tb2.Rows[j];
            //            if (re[0].ToString() == dr[0].ToString())//相同ID则拼接
            //            {
            //                //要保留的
            //                host.Add(i);
            //                client.Add(j);
            //                break;
            //            }
            //        }
            //    }

            //    Intersaction(ref tb2, ref tb4, host, client);

            //    return tb4;
            //}
            //else
            //{
            //    return tb2;
            //}
             DataTable tb3 = (DataTable)combineGridView.DataSource;
             DataSet data = new DataSet();
             data.Tables.Add(tb2);
             return CombineDt(data, tb3);

        }

        /// <summary>
        /// 拼接DT
        /// </summary>
        /// <param name="data">要拼接的数据</param>
        /// <param name="combineDt">原Dt</param>
        /// <returns></returns>
        public static DataTable CombineDt(DataSet data, DataTable combineDt)
        {
            DataTable tb2 = data.Tables[0];
            if (combineDt.Rows.Count > 0)
            {
                DataTable tb4 = new DataTable();//临时表
                tb4 = combineDt.Copy();

                ArrayList host = new ArrayList();
                ArrayList client = new ArrayList();
                for (int i = tb4.Rows.Count - 1; i >= 0; i--)
                {
                    DataRow dr = tb4.Rows[i];
                    for (int j = tb2.Rows.Count - 1; j >= 0; j--)
                    {
                        DataRow re = tb2.Rows[j];
                        if (re[0].ToString() == dr[0].ToString())//相同ID则拼接
                        {
                            //要保留的
                            host.Add(i);
                            client.Add(j);
                            break;
                        }
                    }
                }

                Intersaction(ref tb2,ref tb4, host, client);
               
                return tb4;
            }
            else
            {
                return tb2;
            }
        }

        /// <summary>
        /// 求两个DataTable的交集
        /// </summary>
        /// <param name="tb2">host</param>
        /// <param name="tb4">client</param>
        /// <param name="host">tb4相同的ID索引</param>
        /// <param name="client">tb2相同的ID索引</param>
        public static void Intersaction(ref DataTable tb2,ref DataTable tb4, ArrayList host, ArrayList client)
        {
            for (int i = tb4.Rows.Count - 1; i >= 0; i--)
            {
                if (!host.Contains(i))
                    tb4.Rows.RemoveAt(i);
            }
            for (int i = tb2.Rows.Count - 1; i >= 0; i--)
            {
                if (!client.Contains(i))
                    tb2.Rows.RemoveAt(i);
            }

            AppendDataTable(tb4, tb2);
        }

        /// <summary>
        /// 求两个DataTable的并集
        /// </summary>
        /// <param name="tb2">host</param>
        /// <param name="tb4">client</param>
        /// <param name="host">tb4相同的ID索引</param>
        /// <param name="client">tb2相同的ID索引</param>
        public static void Union(ref DataTable tb2, ref DataTable tb4, ArrayList host, ArrayList client)
        {
            DataTable tb1 = tb4.Clone();//包含tb4中与tb2不同的ID的数据项
            DataTable tb3 = tb2.Clone();//包含tb2中与tb4不同的ID的数据项
            for (int i = tb4.Rows.Count - 1; i >= 0; i--)
            {
                if (!host.Contains(i))
                {
                    tb1.ImportRow(tb4.Rows[i]);                  
                    DataRow drq = tb3.NewRow();
                    tb3.Rows.Add(drq);

                    tb4.Rows.RemoveAt(i);
                }
            }

            for (int i = tb2.Rows.Count - 1; i >= 0; i--)
            {
                if (!client.Contains(i))
                {
                    tb3.ImportRow(tb2.Rows[i]);
                    DataRow drq = tb1.NewRow();
                    tb1.Rows.Add(drq);
   
                    tb2.Rows.RemoveAt(i);
                }
                    
            }

            AppendDataTable(tb4, tb2);
            AppendDataTable(tb1, tb3);

            //tb1加到tb4
            object[] obj = new object[tb4.Columns.Count];

            for (int i = 0; i < tb1.Rows.Count; i++)
            {
                tb1.Rows[i].ItemArray.CopyTo(obj, 0);
                tb4.Rows.Add(obj);
            }
        }

        /// <summary>
        /// 从dataGridView中的选中行或所有行并放到一个新表中
        /// </summary>
        /// <param name="isSelect">是否选中行</param>
        /// <returns>datatable</returns>
        public static DataTable StructrueDataTable(DataGridView dataGridView, bool isSelect)
        {
            #region 从dataGridView中选取行并放到一个新表中，然后再绑定到dataGridView中
            DataTable dataTable = new DataTable();

            int length = 0;

            switch (dataGridView.Name)
            {
                case "rawDataGrid":
                    length = colNum;
                    break;
                case "ndayGrid":
                    length = sumNum;
                    break;
                case "calResultGrid":
                    length = customNum;
                    break;
                case "sectionResultGrid":
                    length = crossNum;
                    break;
                default:
                    length = dataGridView.Columns.Count;
                    break;
            }

            //添加表头
            for (int col = 0; col < length; col++)
            {
                string columnName = dataGridView.Columns[col].Name;
                dataTable.Columns.Add(columnName, dataGridView.Columns[col].ValueType);
            }
            //标题为第一行
            DataRow Row = dataTable.NewRow();
            for (int col = 0; col < length; col++)
            {
                string columnName = dataGridView.Columns[col].HeaderText;
                Row[col] = columnName;
            }
            dataTable.Rows.Add(Row);

            if (isSelect)
            {
                for (int r = dataGridView.SelectedRows.Count - 1; r >= 0; r--)
                {
                    DataRow dataRow = dataTable.NewRow();
                    for (int c = 0; c < length; c++)
                    {
                        dataRow[c] = dataGridView.SelectedRows[r].Cells[c].Value;
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }
            else
            {
                for (int r = 0; r < dataGridView.Rows.Count; r++)
                {
                    DataRow dataRow = dataTable.NewRow();
                    for (int c = 0; c < length; c++)
                    {
                        dataRow[c] = dataGridView.Rows[r].Cells[c].Value;
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }

            return dataTable;
            #endregion
        }

    }
}