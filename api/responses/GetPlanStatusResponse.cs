namespace TrunkBot.Api.Responses
{
    public class GetPlanStatusResponse
    {
        public bool IsFinished { get; set; }
        public bool Succeeded { get; set; }
        public string Explanation { get; set; }
    }
}
