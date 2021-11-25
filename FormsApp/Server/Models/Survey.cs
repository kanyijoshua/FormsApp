using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Surveyapp.Models;

namespace FormsApp.Server.Models
{
    public class Survey
    {
        public Survey()
        {
            SurveyCategorys = new HashSet<SurveyCategory>();
        }
        public int Id { get; set; }
        [Required]
        public string name { get; set; }
        [DataType(DataType.Date)]
        [Required]
        public DateTime Startdate { get; set; }
        [DataType(DataType.Date)]
        [Required]
        public DateTime EndDate { get; set; }
        [Required]
        public string status { get; set; }
        public string approvalStatus { get; set; }
        [Required]
        public string SurveyerId { get; set; }
        [ForeignKey("SurveyerId")]
        public virtual ApplicationUser Surveyer { get; set; }
        public virtual ICollection<SurveyCategory> SurveyCategorys{ get; set; }
    }
}
