using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;


namespace APP.Components.EntityDto
{
    public  class AppEshopShippingAdressDto
    {
        [DataMember]
        public string StreetAddress1
        {
            get; set;
        }

        [DataMember]
        public string StreetAddress2
        {
            get; set;
        }

        [DataMember]
        public string City
        {
            get; set;
        }

        [DataMember]
        public string Province
        {
            get; set;
        }

        [DataMember]
        public string Country
        {
            get; set;
        }

        [DataMember]
        public string County
        {
            get; set;
        }

        [DataMember]
        public string ZipCode
        {
            get; set;
        }

        [DataMember]
        public string PhoneNumber
        {
            get; set;
        }

        [DataMember]
        public string EmailAddress
        {
            get; set;
        }

        [DataMember]
        public string FirstName
        {
            get; set;
        }

        [DataMember]
        public string LastName
        {
            get; set;
        }

    }
}
