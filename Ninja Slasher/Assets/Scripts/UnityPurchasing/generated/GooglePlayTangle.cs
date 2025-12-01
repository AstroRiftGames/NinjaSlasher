// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("Cjh3uiBUhCMudGA4+dV2749tfaFAWanVdxYWWC+KQN54XRf6e1NRkXWrcmN8MN0LCSijZR//DqOpUteitY5WJqN5/xby7BU5u2siz9FRpl6CXEtPbmFqehwQkekEtG07Yiwf87X4DbE7n4+xTNa58cTKQ4gFTKXJuOjZR7beEogO9d2LG6oDx0qx0DHJe/jbyfT/8NN/sX8O9Pj4+Pz5+nv49vnJe/jz+3v4+Pk49+GZRhzpUZAbqiSoSF/EcD+weqZgJDIKlmuNe2gz1p26ns3b1z4IQOuZ5t5yZSqg4cNRg6qb2uNzIFhQpwXEh2eAj03Ugg2ICb2b1v1Qs6hdAwnrWeaKxLk1x/2MCXzigqg2ACcdMjCBFO12c7/h8bAPYPv6+Pn4");
        private static int[] order = new int[] { 9,3,11,10,12,13,6,9,10,12,13,11,13,13,14 };
        private static int key = 249;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
