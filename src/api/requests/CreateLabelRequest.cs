namespace TrunkBot.Api.Requests
{
    public class CreateLabelRequest
    {
        public string Name { get; set; }
        public int Changeset { get; set; }
        public string Comment { get; set; }
    }
}
