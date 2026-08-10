using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Graphics
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("Sunvale/AncientRomeUI/DrapeGraphic")]
    public class DrapeGraphic : MaskableGraphic
    {
        [Header("Art")]
        [SerializeField] private Texture m_Texture;

        [Header("Grid Properties")]
        [Range(1, 20)] public int columns = 5;
        [Range(2, 50)] public int rows = 15;

        [Header("Shader amplitude")]
        [Range(0,2)]
        public float shaderAmiplitudeMult;
        [Range(0,2)]
        public float shaderBaseAmiplitude;
        
        [Header("Authoring (Resting Shape)")]
        public float[] boneOffsets = new float[4] { 0f, 0f, 0f, 0f };

        [Header("Physics Settings (Sway & Spread)")]
        public float tension = 400f;
        public float stiffness = 50f;
        public float damping = 8f;
        [Space(5)]
        public float spreadTension = 150f;
        public float spreadStiffness = 60f;
        public float spreadDamping = 8f;

        [Header("Wind Base Forces")]
        public float baseSwayForce = 800f; 
        public float baseSailForce = 100f; 
        public AnimationCurve gustSpatialDistribution = AnimationCurve.Linear(0, 0.1f, 1, 1f);

        [Header("Overlapping Gust Generator")]
        public bool enableAutoWind = true;
        [Range(1, 10)]
        [Tooltip("How many gusts can overlap at once. 3 to 5 is usually plenty for organic wind.")]
        public int maxOverlappingGusts = 4;
        
        [Tooltip("X = Min Seconds, Y = Max Seconds between SPAWNING a new gust.")]
        public Vector2 gustSpawnInterval = new Vector2(0.5f, 2.0f);
        
        [Tooltip("X = Min Duration, Y = Max Duration of a single gust pulse")]
        public Vector2 gustDuration = new Vector2(1.0f, 3.0f);
        
        [Tooltip("X = Min Intensity, Y = Max Intensity multiplier")]
        public Vector2 gustIntensity = new Vector2(0.3f, 0.8f);

        // --- PHYSICS STATE ---
        private struct SimBone
        {
            public float x; 
            public float vX; 
            public float spread; 
            public float vSpread; 
        }
        private SimBone[] simBones; 

        // --- WIND GUST SYSTEM ---
        private struct Gust
        {
            public bool isActive;
            public float timer;
            public float duration;
            public float intensity;
        }
        
        private Gust[] gusts;
        private int nextGustIndex = 0;
        private float autoWindTimer = 0f;
        
        // Internal animation state
        private bool isAnimating = true;

        public override Texture mainTexture => m_Texture == null ? s_WhiteTexture : m_Texture;
        public Texture texture
        {
            get => m_Texture;
            set
            {
                if (m_Texture == value) return;
                m_Texture = value;
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (material != null)
            {
                material = new Material(material);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitPhysics();
        }

    #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            InitPhysics();
            SetVerticesDirty(); // Ensures resting shape updates in Scene View when tweaking Offsets
        }
    #endif

        /// <summary>
        /// Stops the animation and snaps the mesh back to its un-deformed base position.
        /// </summary>
        public void HaltAnimations()
        {
            isAnimating = false;
            ResetToBasePosition();
        }

        /// <summary>
        /// Resumes the wind and physics simulation.
        /// </summary>
        public void StartAnimations()
        {
            isAnimating = true;
        }
        
        private void InitPhysics()
        {
            int requiredBones = rows + 1;
            if (simBones == null || simBones.Length != requiredBones)
            {
                simBones = new SimBone[requiredBones];
            }
            
            if (gusts == null || gusts.Length != maxOverlappingGusts)
            {
                gusts = new Gust[maxOverlappingGusts];
                nextGustIndex = 0;
            }
        }

        /// <summary>
        /// Clears all forces/velocities and redraws the base mesh.
        /// </summary>
        private void ResetToBasePosition()
        {
            if (simBones != null)
            {
                for (int i = 0; i < simBones.Length; i++)
                {
                    simBones[i].x = 0;
                    simBones[i].vX = 0;
                    simBones[i].spread = 0;
                    simBones[i].vSpread = 0;
                }
            }

            if (gusts != null)
            {
                for (int i = 0; i < gusts.Length; i++)
                {
                    gusts[i].isActive = false;
                    gusts[i].timer = 0;
                }
            }

            // Force unity to redraw the UI element immediately in its resting state
            SetVerticesDirty();
        }

        private void Update()
        {
            if (!isAnimating || !Application.isPlaying)
            {
                return;
            }
            UpdatePhysics();
        }

        private void UpdatePhysics()
        {
            // Cap deltaTime so physics don't explode on lag spikes
            float dt = Mathf.Min(Time.deltaTime, 0.033f);
            bool needsRedraw = false;

            // --- 1. GENERATOR LOGIC ---
            if (enableAutoWind)
            {
                autoWindTimer -= dt;
                if (autoWindTimer <= 0f)
                {
                    TriggerRandomGust();
                    autoWindTimer = Random.Range(gustSpawnInterval.x, gustSpawnInterval.y);
                }
            }

            // --- 2. POLL ACTIVE GUSTS ---
            float accumulatedWindMultiplier = 0f;
            for (int i = 0; i < gusts.Length; i++)
            {
                if (gusts[i].isActive)
                {
                    gusts[i].timer += dt;
                    
                    if (gusts[i].timer >= gusts[i].duration)
                    {
                        gusts[i].isActive = false;
                    }
                    else
                    {
                        float normalizedTime = gusts[i].timer / gusts[i].duration;
                        float timePulse = Mathf.Sin(normalizedTime * Mathf.PI);
                        
                        accumulatedWindMultiplier += timePulse * gusts[i].intensity;
                        needsRedraw = true; 
                    }
                }
            }

            // --- 3. APPLY FORCES ---
            int boneCount = simBones.Length;
            float[] forcesX = new float[boneCount];
            float[] forcesSpread = new float[boneCount];

            for (int y = 0; y < rows; y++) 
            {
                float fX = 0;
                float fSpread = 0;

                // Sway Springs
                fX += -stiffness * simBones[y].x;
                if (y > 0) fX += tension * (simBones[y - 1].x - simBones[y].x);
                fX += tension * (simBones[y + 1].x - simBones[y].x);
                fX += -damping * simBones[y].vX;

                // Spread Springs
                fSpread += -spreadStiffness * simBones[y].spread; 
                if (y > 0) fSpread += spreadTension * (simBones[y - 1].spread - simBones[y].spread);
                fSpread += spreadTension * (simBones[y + 1].spread - simBones[y].spread);
                fSpread += -spreadDamping * simBones[y].vSpread;

                // Apply Wind Force
                if (accumulatedWindMultiplier > 0f)
                {
                    float normalizedY = (float)y / rows; 
                    float spatialProfile = gustSpatialDistribution.Evaluate(1f - normalizedY);

                    float combinedForce = accumulatedWindMultiplier * spatialProfile;
                    
                    fX += baseSwayForce * combinedForce;
                    fSpread += baseSailForce * combinedForce;
                }

                forcesX[y] = fX;
                forcesSpread[y] = fSpread;
            }

            // --- 4. INTEGRATION ---
            for (int y = 0; y < rows; y++)
            {
                simBones[y].vX += forcesX[y] * dt;
                simBones[y].x += simBones[y].vX * dt;

                simBones[y].vSpread += forcesSpread[y] * dt;
                simBones[y].spread += simBones[y].vSpread * dt;

                // Only flag redraw if there's meaningful movement happening
                if (Mathf.Abs(simBones[y].vX) > 0.05f || Mathf.Abs(simBones[y].x) > 0.05f ||
                    Mathf.Abs(simBones[y].vSpread) > 0.05f || Mathf.Abs(simBones[y].spread) > 0.05f)
                {
                    needsRedraw = true;
                }
            }
            
            if (this.material != null) 
            {
                // Pass the gust intensity to the shader flutter amplitude
                
                float dynamicAmplitude = shaderBaseAmiplitude + (accumulatedWindMultiplier * shaderAmiplitudeMult);
                this.material.SetFloat("_FlutterAmplitude", dynamicAmplitude);
            }

            if (needsRedraw) SetVerticesDirty();
        }

        private void TriggerRandomGust()
        {
            if (gusts == null || gusts.Length == 0) return;
            
            gusts[nextGustIndex].isActive = true;
            gusts[nextGustIndex].timer = 0f;
            gusts[nextGustIndex].duration = Random.Range(gustDuration.x, gustDuration.y);
            gusts[nextGustIndex].intensity = Random.Range(gustIntensity.x, gustIntensity.y);
            nextGustIndex = (nextGustIndex + 1) % gusts.Length;
        }

        public void TriggerManualGust()
        {
            if (gusts == null || gusts.Length == 0) return;

            gusts[nextGustIndex].isActive = true;
            gusts[nextGustIndex].timer = 0f;
            gusts[nextGustIndex].duration = Mathf.Lerp(gustDuration.x, gustDuration.y, 0.5f);
            gusts[nextGustIndex].intensity = 1.0f;

            nextGustIndex = (nextGustIndex + 1) % gusts.Length;
        }

        private float GetAuthoringOffset(float normalizedY)
        {
            if (boneOffsets == null || boneOffsets.Length == 0) return 0;
            if (boneOffsets.Length == 1) return boneOffsets[0];
            float t = 1f - normalizedY; 
            float scaledT = t * (boneOffsets.Length - 1);
            int index = Mathf.FloorToInt(scaledT);
            float fraction = scaledT - index;
            if (index >= boneOffsets.Length - 1) return boneOffsets[boneOffsets.Length - 1];
            return Mathf.Lerp(boneOffsets[index], boneOffsets[index + 1], Mathf.SmoothStep(0, 1, fraction));
        }

        // --- MESH DEFORMATION ---
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (simBones == null || simBones.Length != rows + 1) return;

            Rect r = rectTransform.rect;
            float cellWidth = r.width / columns;
            float cellHeight = r.height / rows;
            float centerX = r.center.x;

            for (int y = 0; y <= rows; y++)
            {
                float normalizedY = (float)y / rows; 
                float authOffset = GetAuthoringOffset(normalizedY);
                float swayX = simBones[y].x;
                float spreadAmount = simBones[y].spread;
                float rowScale = (r.width + spreadAmount) / r.width;

                for (int x = 0; x <= columns; x++)
                {
                    UIVertex vertex = UIVertex.simpleVert;
                    float originalLocalX = r.xMin + (x * cellWidth);
                    float localY = r.yMin + (y * cellHeight);

                    float distFromCenter = originalLocalX - centerX;
                    float localX = (centerX + swayX) + (distFromCenter * rowScale);
                    localX += authOffset;

                    vertex.position = new Vector3(localX, localY, 0);
                    float u = (float)x / columns;
                    float v = (float)y / rows;
                    vertex.uv0 = new Vector2(u, v);
                    vertex.color = color;

                    vh.AddVert(vertex);
                }
            }

            int vertsPerRow = columns + 1;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int bottomLeft = (y * vertsPerRow) + x;
                    int topLeft = ((y + 1) * vertsPerRow) + x;
                    int topRight = ((y + 1) * vertsPerRow) + x + 1;
                    int bottomRight = (y * vertsPerRow) + x + 1;
                    vh.AddTriangle(bottomLeft, topLeft, topRight);
                    vh.AddTriangle(bottomLeft, topRight, bottomRight);
                }
            }
        }
    }

}
