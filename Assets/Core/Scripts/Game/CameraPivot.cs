using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Marks the point a view's camera orbits around and zooms towards (see CameraControl).
    /// Place one in the Environment of every view and move it to the middle of what the view shows.
    /// </summary>
    public class CameraPivot : MonoBehaviour
    {
        /// <summary>Looks for the pivot of the view a camera belongs to: the nearest pivot below the camera's ancestors.</summary>
        public static CameraPivot FindFor(Camera camera)
        {
            for (Transform t = camera.transform.parent; t != null; t = t.parent)
            {
                CameraPivot pivot = t.GetComponentInChildren<CameraPivot>(false);
                if (pivot != null)
                    return pivot;
            }
            return null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
