using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExtendedTMPro
{
    public class ExtendedTextMeshProUGUI : TextMeshProUGUI
    {
        [SerializeField]
        private bool enableVertexWarping = true;
        [SerializeField]
        private AnimationCurve vertexCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, -1f), new Keyframe(1f, 0f));
        [SerializeField]
        private float scaleMultiplierPerCharacter = 1f;

        #region UNITY METHODS
        /// <summary>
        /// Called on the frame when a script is enabled just before any of the Update methods is called the first time.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            InitializeCurveWrapModes();
        }

        /// <summary>
        /// Called when the object becomes enabled and active.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            Rebuild(CanvasUpdate.PreRender);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Called when the script is loaded or a value is changed in the inspector (Called in the editor only).
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();

            Rebuild(CanvasUpdate.PreRender);
        }
#endif
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Rebuilds the graphic geometry and its material on the PreRender cycle.
        /// </summary>
        /// <param name="update">The current step of the rendering CanvasUpdate cycle</param>
        public override void Rebuild(CanvasUpdate update)
        {
            base.Rebuild(update);

            if (update == CanvasUpdate.PreRender)
            {
                if (enableVertexWarping)
                {
                    renderMode = TextRenderFlags.DontRender; //Instruct TextMesh Pro to not upload the mesh as we will be modifying it.
                    WarpText();
                }
                else
                {
                    renderMode = TextRenderFlags.Render; //Render text normally.
                }
            }
        }
        #endregion

        #region PRIVATE METHODS
        /// <summary>
        /// Initializes the wrap modes of <see cref="vertexCurve"/>.
        /// </summary>
        private void InitializeCurveWrapModes()
        {
            vertexCurve.preWrapMode = WrapMode.Clamp;
            vertexCurve.postWrapMode = WrapMode.Clamp;
        }

        /// <summary>
        /// Warps the text of this component using <see cref="vertexCurve"/> and <see cref="scaleMultiplierPerCharacter"/>.
        /// </summary>
        private void WarpText()
        {
            havePropertiesChanged = true; //Force the TextMeshPro Object to be updated

            ForceMeshUpdate(); //Generate the mesh and populate the textInfo with data we can use and manipulate
            if (mesh == null)
            { return; }

            int characterCount = textInfo.characterCount;
            if (characterCount == 0 || textInfo.characterInfo.Length < characterCount)
            { return; }

            Vector3[] vertices;
            Matrix4x4 transformationMatrix;

            float boundsMinX = bounds.min.x;
            float boundsMaxX = bounds.max.x;
            float finalCurveScale = scaleMultiplierPerCharacter * characterCount;

            for (int i = 0; i < characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                { continue; }

                int vertexStartIndex = textInfo.characterInfo[i].vertexIndex;

                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                vertices = textInfo.meshInfo[materialIndex].vertices;

                Vector3 offsetToMidBaseline = new Vector2((vertices[vertexStartIndex + 0].x + vertices[vertexStartIndex + 2].x) / 2, textInfo.characterInfo[i].baseLine); //Compute the baseline mid point for each character

                AdjustPivotPoint(vertices, vertexStartIndex, offsetToMidBaseline * -1); //Substract the baseline mid point so that the pivot point is in the correct position before we apply the rotation matrix

                transformationMatrix = CalculatedTransformationMatrix(offsetToMidBaseline, boundsMinX, boundsMaxX, finalCurveScale);
                ApplyTransformationMatrix(vertices, transformationMatrix, vertexStartIndex);

                AdjustPivotPoint(vertices, vertexStartIndex, offsetToMidBaseline); //Now add the baseline mid point again, because we want it back in its original, but transformed/rotated, position
            }

            UpdateVertexData();
        }

        /// <summary>
        /// Computes the angle of rotation for a character based on <see cref="vertexCurve"/>.
        /// </summary>
        /// <param name="offsetToMidBaseline">Baseline mid point of a character</param>
        /// <param name="boundsMinX">Min x bounds of a character</param>
        /// <param name="boundsMaxX">Max x bounds of a character</param>
        /// <param name="finalCurveScale">Amount of scaling to apply to <see cref="vertexCurve"/></param>
        /// <returns>The calculated transformation matrix</returns>
        private Matrix4x4 CalculatedTransformationMatrix(Vector3 offsetToMidBaseline, float boundsMinX, float boundsMaxX, float finalCurveScale)
        {
            float xLeft = (offsetToMidBaseline.x - boundsMinX) / (boundsMaxX - boundsMinX); // Character's position relative to the bounds of the mesh.
            float xRight = xLeft + 0.0001f;
            float yLeft = vertexCurve.Evaluate(xLeft) * finalCurveScale;
            float yRight = vertexCurve.Evaluate(xRight) * finalCurveScale;

            Vector3 horizontal = new Vector3(1, 0, 0);
            Vector3 tangent = new Vector3(xRight * (boundsMaxX - boundsMinX) + boundsMinX, yRight) - new Vector3(offsetToMidBaseline.x, yLeft);

            float dot = Mathf.Acos(Vector3.Dot(horizontal, tangent.normalized)) * Mathf.Rad2Deg;
            Vector3 cross = Vector3.Cross(horizontal, tangent);
            float angle = cross.z > 0 ? dot : 360 - dot;

            return Matrix4x4.TRS(new Vector3(0, yLeft, 0), Quaternion.Euler(0, 0, angle), Vector3.one);
        }

        /// <summary>
        /// Apply offset to adjust our pivot point.
        /// </summary>
        /// <param name="vertices">Vertices that need to be adjusted</param>
        /// <param name="vertexStartIndex">Index of the first Vector3 that needs to be adjusted</param>
        /// <param name="offsetToMidBaseline">Offset that needs to be added to the vertices</param>
        private void AdjustPivotPoint(Vector3[] vertices, int vertexStartIndex, Vector3 offsetToMidBaseline)
        {
            vertices[vertexStartIndex + 0] += offsetToMidBaseline;
            vertices[vertexStartIndex + 1] += offsetToMidBaseline;
            vertices[vertexStartIndex + 2] += offsetToMidBaseline;
            vertices[vertexStartIndex + 3] += offsetToMidBaseline;
        }

        /// <summary>
        /// Applies a 
        /// </summary>
        /// <param name="vertices">Vertices that need to be transformed</param>
        /// <param name="transformationMatrix">Transformation matrix that holds the transformation data</param>
        /// <param name="vertexStartIndex">Index of the first Vector3 that needs to be adjusted</param>
        private void ApplyTransformationMatrix(Vector3[] vertices, Matrix4x4 transformationMatrix, int vertexStartIndex)
        {
            vertices[vertexStartIndex + 0] = transformationMatrix.MultiplyPoint3x4(vertices[vertexStartIndex + 0]);
            vertices[vertexStartIndex + 1] = transformationMatrix.MultiplyPoint3x4(vertices[vertexStartIndex + 1]);
            vertices[vertexStartIndex + 2] = transformationMatrix.MultiplyPoint3x4(vertices[vertexStartIndex + 2]);
            vertices[vertexStartIndex + 3] = transformationMatrix.MultiplyPoint3x4(vertices[vertexStartIndex + 3]);
        }
        #endregion
    }
}