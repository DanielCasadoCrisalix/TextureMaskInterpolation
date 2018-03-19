using UnityEngine;

namespace Crisalix.Visualization
{
    public static class Vector3DebuggerPoints
    {
        public static GameObject CreateSphereInPos(Vector3 p, Color c, string n = "sphere")
        {
            GameObject sphere1 = CreateSphereInPos(p, c, Vector3.up, n);

            return sphere1;
        }

        public static GameObject CreateSphereInPos(Vector3 p, Color c, Vector3 dir, string n = "sphere")
        {
            GameObject sphere1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere1.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            sphere1.transform.position = p;
            sphere1.transform.rotation = Quaternion.LookRotation(dir);
            sphere1.GetComponent<Renderer>().material.color = c;
            sphere1.name = n;
            return sphere1;
        }
        public static GameObject CreateCubeInPos(Vector3 p, Color c, string n = "sphere")
        {
            GameObject sphere1 = CreateCubeInPos(p, c, Vector3.up, n);

            return sphere1;
        }
        public static GameObject CreateCubeInPos(Vector3 p, Color c, Vector3 dir, string n = "sphere")
        {
            GameObject sphere1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sphere1.transform.localScale = new Vector3(1f, 1f, 1f);
            sphere1.transform.position = p;
            sphere1.transform.rotation = Quaternion.LookRotation(dir);
            sphere1.GetComponent<Renderer>().material.color = c;
            sphere1.name = n;
            return sphere1;
        }
    }
}