using System.Collections.Generic;

namespace LSG.SharedKernel.Csv.CsvMapper
{
    public class FishSceneReportCsvMapper : CsvMapper
    {
        Dictionary<string, string> _map;

        protected override Dictionary<string, string> Map =>
            _map ??= new Dictionary<string, string>
            {
                {"StartTime", "場景開始時間"},
                {"EndTime", "場景結束時間"},
                {"Bet", "投注"},
                {"WinLose", "公司输赢"},
                {"UserName", "用户名"},
                {"Shift", "班別"},
            };
    }
}