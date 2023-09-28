using System;
namespace CopaParkour.Models
{
    public class EntryDto
    {

        public string Name { get; set; }

        public string Phone { get; set; }

        public Categories Category { get; set; }

        public string? Email { get; set; }


        public override string ToString()
        {
            return $"{{\"Name\": \"{Name}\", \"Phone\": {Phone}, " +
                $"\"Category\": \"{Category}\", \"Email\": \"{Email}\"}}";
        }
    }
}

