using System.Collections.Generic;

namespace LSG.SharedKernel.Csv.CsvMapper
{
    public class FishReportCsvMapper : CsvMapper
    {
        Dictionary<string, string> _map;

        protected override Dictionary<string, string> Map =>
            _map ??= new Dictionary<string, string>
            {
                {"Date", "日期"},
                {"BetUserCount", "投注人数"},
                {"TotalBet", "总投注"},
                {"TotalWinLose", "公司净输赢"},
            };
    }
}