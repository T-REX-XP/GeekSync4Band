using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using GeekSync4Band.Model;
using GeekSync4Band.Properties;


namespace GeekSync4Band.Manager
{
    class DbManager
    {
        private static DbManager _instance;
        private readonly SQLiteConnection Connection = new SQLiteConnection("Data Source=" + DbFile);


        public static DbManager Instance()
        {
            return _instance ?? (_instance = new DbManager());
        }

        protected DbManager()
        {
            if (!File.Exists(DbFile))
            {
                CreateDatabase();
            }
        }


        private static string DbFile
        {
            get { return Environment.CurrentDirectory + "\\" + Settings.Default["db_file"]; }
        }

        public void RemoveDevice(string mac)
        {
            var resultSql = "DELETE FROM Devices WHERE d_mac ='{0}'";
            resultSql = string.Format(resultSql, mac);
            var result = Connection.Query<long>(resultSql);
        }

        private void CreateDatabase()
        {
            var DeviseDB = @"CREATE TABLE [devices] " +
                           "(" +
                           "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL, " +
                           "[d_name] nvarchar UNIQUE, " +
                           "[d_brand] nvarchar, " +
                           "[d_mac] nvarchar UNIQUE, " +
                           "[d_color] nvarchar, " +
                           "[d_weight] INT, " +
                           "[d_height] INT, " +
                           "[d_sex] BOOLEAN," +
                           "[d_age] INT," +
                           "[d_goal] INT," +
                           "[d_fw] INT" +
                           ")";

            var StepsDB = @"CREATE TABLE [steps] " +
                          "(" +
                          "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL, " +
                          "[s_devmac] VARCHAR, " +
                          "[s_year] INT, " +
                          "[s_month] INT, " +
                          "[s_date] DATETIME, " +
                          "[s_hour] INT, " +
                          "[s_steps] long, " +
                          "[s_distance] long, " +
                          "[s_calories] long" +
                          ")";

            Connection.Open();
            Connection.Execute(DeviseDB, StepsDB);
            Connection.Execute(StepsDB, DeviseDB);

        }

        public int AddDevice(ref DBDevice device)
        {
            // Connection.Open();
            device.Id = Connection.Query<int>(
                @"INSERT INTO Devices 
            ( d_name, d_brand, d_mac, d_color, d_weight, d_height, d_sex, d_age, d_goal, d_fw) VALUES 
            ( @d_name, @d_brand, @d_mac, @d_color, @d_weight, @d_height, @d_sex, @d_age, @d_goal, @d_fw );
            select last_insert_rowid()", device).First();
            return device.Id;
        }

        public DBDevice GetDevice(string mac)
        {
            //  Connection.Open();
            DBDevice result = Connection.Query<DBDevice>(
                @"SELECT d_name, d_mac, d_weight, d_height, d_sex, d_age, d_goal, d_fw
                      FROM Devices
                      WHERE d_mac = @mac", new { mac }).FirstOrDefault();
            return result;
        }

        public void ProcessDevice(ref DBDevice device)
        {
            var dev = GetDevice(device.d_mac);
            if (dev == null)
                device.Id = AddDevice(ref device);
            else
                device.Id = dev.Id;
        }

        public List<DBDevice> GetListDevices()
        {
            var list = new List<DBDevice>();
            // int i = 0;
            var result = Connection.Query<DBDevice>(
                @"SELECT d_name,d_brand, d_mac, d_weight, d_height, d_sex, d_age, d_goal, d_fw 
                      FROM Devices ").ToList();
            list.AddRange(result);
            return list;
        }

        public List<DBStep> GetGoalsForWeek()
        {
            var list = new List<DBStep>();
            //   int i = 0;
         
            
                var result = Connection.Query<DBStep>(
                @"SELECT s_devmac, s_steps, s_date, s_distance, s_calories "+
                 "FROM( SELECT s_devmac, sum(s_steps) as s_steps, s_date, sum(s_distance) as s_distance, sum(s_calories) as s_calories "+
                 "FROM Steps GROUP BY s_date ORDER BY s_date DESC LIMIT 7 ) "+
                 "T1 ORDER BY s_date").ToList();
            /*
            var result = Connection.Query<DBStep>(
                @"SELECT s_devmac, sum(s_steps) as s_steps, s_date, sum(s_distance) as s_distance, sum(s_calories) as s_calories
                      FROM Steps 
                      GROUP BY s_date ORDER BY s_date LIMIT 7 ").ToList();
            */
            
            list.AddRange(result);
            return list;
        }

        public bool SyncSteps(ref IDevice band)
        {
            var rawStepData = band.getSptdata();
            try
            {

                // tbSportData.Text = "[День] [Час] [Шагов] [Дистанция] [Калорий]\r\n";
                for (int i = 0; i < 0xa8; i++)
                {
                    DBStep step = new DBStep();
                    step.s_date = DateTime.Parse(rawStepData[i, 0]); //int.Parse(rawStepData[i, 0]);
                    step.s_hour = int.Parse(rawStepData[i, 1]);
                    step.s_steps = long.Parse(rawStepData[i, 2]);
                    step.s_distance = long.Parse(rawStepData[i, 3]);
                    step.s_calories = long.Parse(rawStepData[i, 4]);
                    step.s_devmac = band.CurrentInfo.d_mac;
                    if (step.s_steps != 0 && !IsStepsSynced(step.s_devmac, step.s_steps, DateTimeSQLite(step.s_date)))
                        AddStepInDb(step);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                // throw;
            }
            return true;
        }

        public bool IsStepsSynced(string mac, long s_steps, string s_date)
        {
            var resultSql = "SELECT count(*) FROM Steps WHERE s_devmac ='{0}' AND s_date='{1}' AND s_steps ={2}";
            resultSql = string.Format(resultSql, mac, s_date, s_steps);
            var result = Connection.Query<long>(resultSql).First();
            if (result >0)
            return true;
            else
            {
                return false;
            }
        }

        public int AddStepInDb(DBStep stepLog)
        {
            if (stepLog.s_steps != 0)
            {
                //  string dat = "";
                // dat = DateTimeSQLite(stepLog.s_date);
                stepLog.Id = Connection.Query<int>(
                    @"INSERT INTO Steps 
            ( s_devmac, s_year, s_month, s_date, s_hour, s_steps, s_distance, s_calories) VALUES 
            ( @s_devmac, @s_year, @s_month, @s_date, @s_hour, @s_steps, @s_distance, @s_calories );
            select last_insert_rowid()", stepLog).First();

            }
            return stepLog.Id;
        }

        public List<DBStep> GetListStepByMac(string mac)
        {
            var list = new List<DBStep>();
            //   int i = 0;
            var result = Connection.Query<DBStep>(
                @"SELECT s_devmac, sum(s_steps) as s_steps,s_date, s_hour, sum(s_distance) as s_distance , sum(s_calories) as s_calories
                      FROM Steps 
                      WHERE s_devmac =@mac GROUP  BY s_date", new { mac }).ToList();
            list.AddRange(result);
            return list;
        }


        private string DateTimeSQLite(DateTime datetime)
        {
            var month = "";
            var day = "";
            string dateTimeFormat = "{0}-{1}-{2} {3}:{4}:{5}";
            if (datetime.Month.ToString(CultureInfo.InvariantCulture).Length == 1)
                month = "0" + datetime.Month;
            else
            {
                month = datetime.Month.ToString(CultureInfo.InvariantCulture);
            }
            if (datetime.Day.ToString().Length == 1)
                day = "0" + datetime.Day;
            else
            {
                day = datetime.Day.ToString(CultureInfo.InvariantCulture);
            }

            var result = string.Format(dateTimeFormat, datetime.Year, month, day, "00", "00", "00");
            return result;
            //   return string.Format(dateTimeFormat, datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, datetime.Millisecond);
        }
        public long GetStepsByDate(DateTime dateTime, string mac)
        {
            string dat = DateTimeSQLite(dateTime);
            string sql = @"SELECT s_devmac, sum(s_steps) as s_steps, s_distance, s_calories
                      FROM Steps 
                      WHERE s_devmac ='{0}' AND  s_date = '{1}'";
            sql = string.Format(sql, mac, dat);
            var result = Connection.Query<DBStep>(sql).First();
            //   list.AddRange(result);
            return result.s_steps;
        }

    }
}
