using System;
using System.Collections.Generic;
using System.Text;

namespace Symantec.CWoC.PatchTrending {
    public static class Counters {
        private static int _jspages;
        private static int _htmlpages;
        private static int _sqlqueries;

        public static int Pages {
            get { return _htmlpages + _jspages; }
        }

        public static int JsPages {
            get { return _jspages; }
            set { _jspages = value; }
        }
        

        public static int HtmlPages {
            get { return _htmlpages;  }
            set { _htmlpages = value; }
        }

        public static int SqlQueries {
            get { return _sqlqueries; }
            set { _sqlqueries = value; }
        }

        public static void Init() {
            _jspages = 0;
            _htmlpages = 0;
            _sqlqueries = 0;
        }

        
    }
}
