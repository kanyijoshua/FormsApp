using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Surveyapp.Models;

namespace FormsApp.Server.Models
{
    public class ApplicationUser: IdentityUser
    {
        public ApplicationUser()
        {
            Surveys = new HashSet<Survey>();
            SurveyResponses = new HashSet<SurveyResponse>();
        }
        public virtual ICollection<Survey> Surveys { get; set; }
        public virtual ICollection<SurveyResponse> SurveyResponses { get; set; }
    }
}
