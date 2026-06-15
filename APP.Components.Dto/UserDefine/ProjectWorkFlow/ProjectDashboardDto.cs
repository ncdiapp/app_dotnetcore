using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using APP.Framework;
using APP.Framework.Collections;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{

    public partial class ProjectDashboardDto
    {

        //NbTotalTaskPlannedCompletedDays / NbTotalTaskDays
        [DataMember(EmitDefaultValue = false)]
        public double PlannedCompletion
        {
            get;
            set;
        }

        //NbTotalTaskActualCompletedDays / NbTotalTaskDays
        [DataMember(EmitDefaultValue = false)]
        public double ActualCompletion
        {
            get;
            set;
        }


        //ActualCompletion - PlannedCompletion
        [DataMember(EmitDefaultValue = false)]
        public double Slippage
        {
            get;
            set;
        }

        //Time: Time shows what percentage your project is behind schedule.   
        // = Slippage    
        [DataMember(EmitDefaultValue = false)]
        public string TimeHealthDisplay
        {
            get;
            set;
        }


        //Tasks: Shows how many tasks still need to be completed.     
        [DataMember(EmitDefaultValue = false)]
        public double TasksHealth
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TasksHealthDisplay
        {
            get;
            set;
        }


        //Workload: shows how many tasks are overdue.       
        [DataMember(EmitDefaultValue = false)]
        public double NbOverdueTasks
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string WorkloadHealthDisplay
        {
            get;
            set;
        }

       

        //Progress: Shows what percentage of your project is complete.           
        [DataMember(EmitDefaultValue = false)]
        public string ProgressHealthDisplay
        {
            get;
            set;
        }



        //Cost: Shows your costs vs your planned budget.
        [DataMember(EmitDefaultValue = false)]
        public double CostHealth
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string CostHealthDisplay
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int TotalNbTasks
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int NbNotStartedTasks
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int NbCompletedTasks
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int NbInProgressTasks
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public double NbTotalTaskPlannedCompletedDays
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public double NbTotalTaskActualCompletedDays
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public double NbTotalTaskDays
        {
            get;
            set;
        }




        [DataMember(EmitDefaultValue = false)]
        public double PlannedProjectCost
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public double ActualProjectCost
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public double ProjectBudget
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public List<ProjectDashboardTaskDto> ProjectTaskList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<ProjectDashboardUserDto> ProjectUserList
        {
            get;
            set;
        }



    }


    public partial class ProjectDashboardTaskDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int TaskId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Display
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? Sort
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public double PlannedCompletePercentage
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public double ActualCompletePercentage
        {
            get;
            set;
        }
    }

    public partial class ProjectDashboardUserDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int UserId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Display
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int NbCompletedTasks
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int NbRemainingTasks
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int NbOverdueTasks
        {
            get;
            set;
        }
    }
}

