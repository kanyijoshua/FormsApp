using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Surveyapp.Models;

namespace FormsApp.Server.Models
{
    public class SurveySubject
    {
        public SurveySubject()
        {
            Questions = new HashSet<Question>();
            //ResponseTypes = new ResponseType();
        }
        [Key]
        public int Id { get; set; }
        [Required]
        public string SubjectName { get; set; }
        [Column(TypeName = "jsonb")]
        public Dictionary<string, string> OtherProperties { get; set;}
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual SurveyCategory Category { get; set; }
        public virtual ICollection<Question> Questions { get; set; }
        public virtual ResponseType ResponseTypes { get; set; }
    }
}
