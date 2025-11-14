using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UploadRecords.Models;
using UploadRecords.Services;

namespace UploadRecords.Utils
{
    public static class Division
    {
        public static List<DivisionData> GetDivisionDatas(List<DivisionConfiguration> config, CSDB csdb) {
            List<DivisionData> result = [];

            foreach (var item in config)
            {
                var preps = csdb.GetKuafsByNames(item.Preps);
                result.Add(new DivisionData
                {
                    Name = item.Name,
                    Preps = item.Preps,
                    PrepDatas = preps
                });
            }
            
            return result; 
        }
    }
}
