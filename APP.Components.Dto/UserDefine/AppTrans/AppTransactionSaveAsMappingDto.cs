using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using Newtonsoft.Json;

namespace APP.Components.EntityDto
{
    /// <summary>
    /// DTO class for the entity 'AppTransactionSaveAsMapping'.
    /// </summary>


    public partial class AppTransactionSaveAsMappingDto
    {

        //AppMessageTransfer.ToList && o.TransactionFieldId
        public string DestinationInternalCode
        {
            get 
            {
                return this.Name;
            }

            set
            {
                this.Name = value;

            }
           


        }

        public int? TransactionFieldId
        {
            get
            {
                return this.SourceFiledId;
            }

            set
            {
                this.SourceFiledId = value;

            }


        }

        public int? SourceUnitId
        {
            get;
            set;
        }

        public AppTransactionUnitExDto SourceUnitExDto
        {
            get;
            set;
        }

        public AppTransactionFieldExDto SourceTransactionFieldExDto
        {
            get;
            set;
        }

        public int? TargetUnitId
        {
            get;
            set;
        }

        public AppTransactionUnitExDto TargetUnitExDto
        {
            get;
            set;
        }

        public AppTransactionFieldExDto TargetTransactionFieldExDto
        {
            get;
            set;
        }


        // Json: Manufacturers[0].TotalPrice, 
        // ChildArrayPathName: Manufacturers;
        // ChildArraySubPropertyPathName: TotalPrice              

        // Json: Manufacturers[0].Products[0].Price, 
        // GrandChildArrayPathName: Products              
        // GrandChildArraySubPropertyPathName: Price               

        public string ChildArrayPathName
        {
            get;
            set;
        }
       
        public string ChildArraySubPropertyPathName
        {
            get;
            set;
        }
       
        public string GrandChildArrayPathName
        {
            get;
            set;
        }
      
        public string GrandChildArraySubPropertyPathName
        {
            get;
            set;
        }
    }
}

