namespace APP.Framework.Communication
{
    /// <summary>
    /// Represents a client identity
    /// </summary>
    public interface IClientIdentity
    {
        /// <summary>
        /// Gets the session id.
        /// </summary>
        object SessionId { get; }



        /// <summary>
        /// Gets the user id.
        /// </summary>
        object UserId { get; }


		object CurrentWorkingCompanyId { get; }


        string CurrentUserDbConnectionString
        {
            get;
            set;
        }

         int? CurrentPartnerId
        {
            get;
            set;
        }

        int DataSourceId
		{
			get;
			set;
		}

        //SSA (SYSTEM SA),ASA ( APP SA), EMPLOEE, SUPPLIER, CUSTOMER
      
        int CurrentLoginUserType
        {
            get;
            set;
        }

        //object RuningTimeBusinessAccountId
        //{
        //    get;
        //    set;
        //}

        

        object LanguageId { get; }
        /// <summary>
        /// Determines whether this instance is valid.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </returns>
        bool IsValid();
    }
}