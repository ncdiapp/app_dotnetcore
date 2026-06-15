using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using System.Data;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppChildDataDto
    {

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, object> DictOneToOneFields
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<string> CascadingNeedToBeLockedFields
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? CascadingUnitId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? CascadingFieldId
        {
            get;
            set;
        }

        //key1:child unit fieldid  
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, List<LookupItemDto>> DictCascadingFiledDataSource
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, List<LookupItemDto>> DictAutoCompleteFieldDataSource
        {
            get;
            set;
        }




        [DataMember]
        public bool IsDirty
        {
            get;
            set;
        }

        [DataMember]
        public bool IsNew
        {
            get;
            set;
        }



        // Key:  GrandChild Tanscation unit ID .tostring()
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<String, List<AppChildDataDto>> DictOneToManyFields
        {
            get;
            set;
        }

     

        [DataMember(EmitDefaultValue = false)]
        public List<String>  ListCascadingAvailableDataChangeUnitIds
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<String> ListSubscribeDataChangeUnitIds
        {
            get;
            set;
        }
        //// Key:  GrandChild Tanscation unit ID .tostring()
        //[DataMember(EmitDefaultValue = false)]
        //public Dictionary<String, List<Dictionary<string, object>>> DictOneToManyFields
        //{
        //    get;
        //    set;
        //}
        // Key:TTanscation unit ID
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<String, List<object>> DictDirtyGrandChildrenPK
        {
            get;
            set;
        }

        //Key:GrandChild  unit ID , second key: GrandChild  unit fieldID namr,thirdKey: GrandChild  unit rowidentityKey
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<String, Dictionary<String, Dictionary<string, List<LookupItemDto>>>> DictGrandCascadingFiledDataSource
        {
            get;
            set;
        }




        //Key:GrandChild  unit ID  newadd, modifyed, delete
        [IgnoreDataMember] 
        public  Dictionary<string,         Tuple<List<Dictionary<string, object>>, List<Dictionary<string, object>>, List<Dictionary<string, object>>> > DictGrandChildChangedCollection
        {
            get;
            set;
        }

        // newadd, modifyed, delete
        //[IgnoreDataMember]
        //public bool IsGrandChildChangedCollection
        //{
        //    get
        //    {
        //        if (GrandChildChangedCollection != null)
        //        {
        //            return (
        //                      (!GrandChildChangedCollection.Item1.IsEmpty())
        //                    || (!GrandChildChangedCollection.Item2.IsEmpty())
        //                    || (!GrandChildChangedCollection.Item3.IsEmpty())
        //                );

        //        }

        //        return false;
        //    }
           
        //}
        [DataMember]
        public object  ChildUnitId
        {
            get;
            set;
        }

       
        public string PKValueCombinString
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsSelected
        {
            get;
            set;
        }
    }
}
