// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("57eGGOmBTddRqoLURPVcmBXuj24fBvaKKElJB3DVH4EnAkilJAwOznX/vpwO3PXEhbwsfwcP+Fqb2Djf3QMUEDE+NSVDT862W+syZD1zQKzqp1LuZMDQ7hOJ5q6blRzXWhP6llVnKOV/C9t8cSs/Z6aKKbDQMiL+Ds9E9Xv3FwCbL2DvJfk/e21VyTTVm+ZqmKLTViO93fdpX3hCbW/eS+rRCXn8JqBJrbNKZuQ0fZCODvkBJKepppYkp6ykJKenpmeovsYZQ7aWJKeElqugr4wg7iBRq6enp6OmpdIkN2yJwuXBkoSIYVcftMa5gS060BKL3VLXVuLEiaIP7PcCXFa0Brkq9C08I2+CVFZ3/DpAoFH89g2I/bIpLOC+ru9QP6Slp6an");
        private static int[] order = new int[] { 6,3,2,12,13,9,13,7,10,12,13,12,13,13,14 };
        private static int key = 166;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
