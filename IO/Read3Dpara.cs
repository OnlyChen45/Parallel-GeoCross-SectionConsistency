using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace ThreeDModelSystemForSection.DataIOWorker
{
  public  class Read3Dpara
    {
        public static void makeparaIntoFrom(string txtpath,out Dictionary<string,double> resultpara) {
            resultpara = new Dictionary<string, double>();
            FileStream file = new FileStream(txtpath, FileMode.Open);
            StreamReader reader = new StreamReader(file);
            List<string> stt = new List<string>();
            while (!reader.EndOfStream) {
                stt.Add( reader.ReadLine());
            }
            string[] lines = stt.ToArray();
            foreach (string st in lines)
            {
                string[] kv = st.Split(':');
                resultpara.Add(kv[0], double.Parse(kv[1]));
            }
        }
    }
}
