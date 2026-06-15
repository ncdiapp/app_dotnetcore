using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APP.Components.Dto
{
    public class AsynCallPool
    {



        public int Count
        {
            get;
            set;
        }

        public int CompleteedCount
        {
            get;
            set;
        }

        public Action FinalCallBack
        {
            get;
            set;
        }


        public AsynCallPool(int count, Action finalCallBack)
        {
            Count = count;
            FinalCallBack = finalCallBack;
            CompleteedCount = 0;



        }
        public void TryTriggerCallBack()
        {
            CompleteedCount++;
            if (CompleteedCount >= Count)
            {

                FinalCallBack();
            }
            //if(CompleteedCount

        }



    }
}
