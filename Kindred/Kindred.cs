using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy.SDK.Events;

namespace Kindred
{
    public static class Kindred
    {
        static Kindred()
        {
            Loading.OnLoadingComplete += OnLoadingComplete;
        }

        public static void Main(string[] args)
        {
        }

        private static void OnLoadingComplete(EventArgs args)
        {

        }
    }
}
