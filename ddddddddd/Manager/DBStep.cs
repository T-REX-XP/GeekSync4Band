using System;

namespace GeekSync4Band.Manager
{
    public class DBStep
    {
        public int Id { get; set; }
        public string s_devmac { get; set; }
        public int s_year { get; set; }
        public int s_month { get; set; }
        public DateTime s_date { get; set; }
        public int s_hour { get; set; }
        public long s_steps { get; set; }
        public long s_distance { get; set; }
        public long s_calories { get; set; }
    }
}