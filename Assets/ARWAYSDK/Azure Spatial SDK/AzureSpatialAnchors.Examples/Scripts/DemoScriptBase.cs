// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arway;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    public abstract class DemoScriptBase : InputInteractionBase
    {
        #region Member Variables
        private Task advanceDemoTask = null;
        protected bool isErrorActive = false;
        protected Text feedbackBox;
        protected readonly List<string> anchorIdsToLocate = new List<string>();
        protected AnchorLocateCriteria anchorLocateCriteria = null;
        protected CloudSpatialAnchor currentCloudAnchor;
        protected CloudSpatialAnchorWatcher currentWatcher;
        protected GameObject spawnedObject = null;
        protected bool enableAdvancingOnSelect = true;

        protected HProgressBar m_ProgressBar;

        #endregion // Member Variables

        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("The prefab used to represent an anchored object.")]
        private GameObject anchoredObjectPrefab = null;

        [SerializeField]
        [Tooltip("SpatialAnchorManager instance to use for this demo. This is required.")]
        private SpatialAnchorManager cloudManager = null;

        [SerializeField]
        [Tooltip("ARSpace Gameobject for holding Anchors. This is required.")]
        public GameObject ARSpace;
        #endregion // Unity Inspector Variables

        /// <summary>
        /// Destroying the attached Behaviour will result in the game or Scene
        /// receiving OnDestroy.
        /// </summary>
        /// <remarks>OnDestroy will only be called on game objects that have previously been active.</remarks>
        public override void OnDestroy()
        {
            if (CloudManager != null)
            {
                CloudManager.StopSession();
            }

            if (currentWatcher != null)
            {
                currentWatcher.Stop();
                currentWatcher = null;
            }

            CleanupSpawnedObjects();

            // Pass to base for final cleanup
            base.OnDestroy();
        }

        public virtual bool SanityCheckAccessConfiguration()
        {
            Debug.Log("osama xx sanitycheck");
            CloudManager.SpatialAnchorsAccountId = "69d870f5-e064-4156-a8fc-9f63b78ef8bf";
            CloudManager.SpatialAnchorsAccountKey = "WP7jKGGRAlxSLDZ/76BBxma+pC0ScvU2JBeptffQB2k=";
            CloudManager.SpatialAnchorsAccountDomain = "eastus.mixedreality.azure.com";
            CloudManager.TenantId = "ad2a8324-bef7-46a8-adb4-fe51b6613b24";
            if (string.IsNullOrWhiteSpace(CloudManager.SpatialAnchorsAccountId)
                || string.IsNullOrWhiteSpace(CloudManager.SpatialAnchorsAccountKey)
                || string.IsNullOrWhiteSpace(CloudManager.SpatialAnchorsAccountDomain))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any
        /// of the Update methods are called the first time.
        /// </summary>
        public override void Start()
        {
            feedbackBox = XRUXPicker.Instance.GetFeedbackText();

            m_ProgressBar = XRUXPicker.Instance.getProgressBar();

            if (feedbackBox == null)
            {
                Debug.Log($"{nameof(feedbackBox)} not found in scene by XRUXPicker.");
                Destroy(this);
                return;
            }

            if (CloudManager == null)
            {
                Debug.Break();
                Debug.Log($"{nameof(CloudManager)} reference has not been set. Make sure it has been added to the scene and wired up to {this.name}.");
                return;
            }

            if (!SanityCheckAccessConfiguration())
            {
                Debug.Log($"{nameof(SpatialAnchorManager.SpatialAnchorsAccountId)}, {nameof(SpatialAnchorManager.SpatialAnchorsAccountKey)} and {nameof(SpatialAnchorManager.SpatialAnchorsAccountDomain)} must be set on {nameof(SpatialAnchorManager)}");
            }

            if (AnchoredObjectPrefab == null)
            {
                Debug.Log("CreationTarget must be set on the demo script.");
                return;
            }

            CloudManager.SessionUpdated += CloudManager_SessionUpdated;
            CloudManager.AnchorLocated += CloudManager_AnchorLocated;
            CloudManager.LocateAnchorsCompleted += CloudManager_LocateAnchorsCompleted;
            CloudManager.LogDebug += CloudManager_LogDebug;
            CloudManager.Error += CloudManager_Error;

            anchorLocateCriteria = new AnchorLocateCriteria();

            base.Start();
        }

        /// <summary>
        /// Advances the demo.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        public abstract Task AdvanceDemoAsync();

        /// <summary>
        /// This version only exists for Unity to wire up a button click to.
        /// If calling from code, please use the Async version above.
        /// </summary>
        public async void AdvanceDemo()
        {
            try
            {
                advanceDemoTask = AdvanceDemoAsync();
                await advanceDemoTask;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(DemoScriptBase)} - Error in {nameof(AdvanceDemo)}: {ex.Message} {ex.StackTrace}");
                feedbackBox.text = $"Demo failed, check debugger output for more information";
            }
        }

        public virtual Task EnumerateAllNearbyAnchorsAsync() { throw new NotImplementedException(); }

        public async void EnumerateAllNearbyAnchors()
        {
            try
            {
                await EnumerateAllNearbyAnchorsAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(DemoScriptBase)} - Error in {nameof(EnumerateAllNearbyAnchors)}: === {ex.GetType().Name} === {ex.ToString()} === {ex.Source} === {ex.Message} {ex.StackTrace}");
                feedbackBox.text = $"Enumeration failed, check debugger output for more information";
            }
        }

        /// <summary>
        /// returns to the launcher scene.
        /// </summary>
        public async void ReturnToLauncher()
        {
            // If AdvanceDemoAsync is still running from the gesture handler,
            // wait for it to complete before returning to the launcher.
            if (advanceDemoTask != null) { await advanceDemoTask; }

            // Return to the launcher scene
            SceneManager.LoadScene(0);
        }

        /// <summary>
        /// Cleans up spawned objects.
        /// </summary>
        protected virtual void CleanupSpawnedObjects()
        {
            // if (spawnedObject != null)
            // {
            //     Destroy(spawnedObject);
            //     spawnedObject = null;
            // }
        }

        protected CloudSpatialAnchorWatcher CreateWatcher()
        {
            if ((CloudManager != null) && (CloudManager.Session != null))
            {
                return CloudManager.Session.CreateWatcher(anchorLocateCriteria);
            }
            else
            {
                return null;
            }
        }

        protected void SetAnchorIdsToLocate(IEnumerable<string> anchorIds)
        {
            if (anchorIds == null)
            {
                throw new ArgumentNullException(nameof(anchorIds));
            }

            anchorIdsToLocate.Clear();
            anchorIdsToLocate.AddRange(anchorIds);

            string[] anchoriden = anchorIdsToLocate.ToArray();

            anchorLocateCriteria.Identifiers = anchoriden;
        }

        protected void ResetAnchorIdsToLocate()
        {
            anchorIdsToLocate.Clear();
            anchorLocateCriteria.Identifiers = new string[0];
        }

        protected void SetNearbyAnchor(CloudSpatialAnchor nearbyAnchor, float DistanceInMeters, int MaxNearAnchorsToFind)
        {
            if (nearbyAnchor == null)
            {
                anchorLocateCriteria.NearAnchor = new NearAnchorCriteria();
                return;
            }

            NearAnchorCriteria nac = new NearAnchorCriteria();
            nac.SourceAnchor = nearbyAnchor;
            nac.DistanceInMeters = DistanceInMeters;
            nac.MaxResultCount = MaxNearAnchorsToFind;

            anchorLocateCriteria.NearAnchor = nac;
        }

        protected void SetNearDevice(float DistanceInMeters, int MaxAnchorsToFind)
        {
            NearDeviceCriteria nearDeviceCriteria = new NearDeviceCriteria();
            nearDeviceCriteria.DistanceInMeters = DistanceInMeters;
            nearDeviceCriteria.MaxResultCount = MaxAnchorsToFind;

            anchorLocateCriteria.NearDevice = nearDeviceCriteria;
        }

        protected void SetGraphEnabled(bool UseGraph, bool JustGraph = false)
        {
            anchorLocateCriteria.Strategy = UseGraph ?
                                            (JustGraph ? LocateStrategy.Relationship : LocateStrategy.AnyStrategy) :
                                            LocateStrategy.VisualInformation;
        }

        /// <summary>
        /// Bypassing the cache will force new queries to be sent for objects, allowing
        /// for refined poses over time.
        /// </summary>
        /// <param name="BypassCache"></param>
        public void SetBypassCache(bool BypassCache)
        {
            anchorLocateCriteria.BypassCache = BypassCache;
        }

        /// <summary>
        /// Determines whether the demo is in a mode that should place an object.
        /// </summary>
        /// <returns><c>true</c> to place; otherwise, <c>false</c>.</returns>
        protected abstract bool IsPlacingObject();

        /// <summary>
        /// Moves the specified anchored object.
        /// </summary>
        /// <param name="objectToMove">The anchored object to move.</param>
        /// <param name="worldPos">The world position.</param>
        /// <param name="worldRot">The world rotation.</param>
        /// <param name="cloudSpatialAnchor">The cloud spatial anchor.</param>
        protected virtual void MoveAnchoredObject(GameObject objectToMove, Vector3 worldPos, Quaternion worldRot, CloudSpatialAnchor cloudSpatialAnchor = null)
        {
            // Get the cloud-native anchor behavior
            CloudNativeAnchor cna = objectToMove.GetComponent<CloudNativeAnchor>();

            // Warn and exit if the behavior is missing
            if (cna == null)
            {
                Debug.LogWarning($"The object {objectToMove.name} is missing the {nameof(CloudNativeAnchor)} behavior.");
                return;
            }

            // Is there a cloud anchor to apply
            if (cloudSpatialAnchor != null)
            {
                // Yes. Apply the cloud anchor, which also sets the pose.
                Debug.Log("setting pose internally ***********");
                cna.CloudToNative(cloudSpatialAnchor);
                // cna.SetPose(worldPos, worldRot);
            }
            else
            {
                // No. Just set the pose.
                cna.SetPose(worldPos, worldRot);
            }
        }

        /// <summary>
        /// Called when a cloud anchor is located.
        /// </summary>
        /// <param name="args">The <see cref="AnchorLocatedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
        {
            // To be overridden.
        }

        /// <summary>
        /// Called when cloud anchor location has completed.
        /// </summary>
        /// <param name="args">The <see cref="LocateAnchorsCompletedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnCloudLocateAnchorsCompleted(LocateAnchorsCompletedEventArgs args)
        {
            Debug.Log("Locate pass complete");
        }

        /// <summary>
        /// Called when the current cloud session is updated.
        /// </summary>
        protected virtual void OnCloudSessionUpdated()
        {
            // To be overridden.
        }

        /// <summary>
        /// Called when gaze interaction occurs.
        /// </summary>
        protected override void OnGazeInteraction()
        {
#if WINDOWS_UWP || UNITY_WSA
            // HoloLens gaze interaction
            if (IsPlacingObject())
            {
                base.OnGazeInteraction();
            }
#endif
        }

        /// <summary>
        /// Called when gaze interaction begins.
        /// </summary>
        /// <param name="hitPoint">The hit point.</param>
        /// <param name="target">The target.</param>
        protected override void OnGazeObjectInteraction(Vector3 hitPoint, Vector3 hitNormal)
        {
            base.OnGazeObjectInteraction(hitPoint, hitNormal);

#if WINDOWS_UWP || UNITY_WSA
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hitNormal);
            SpawnOrMoveCurrentAnchoredObject(hitPoint, rotation);
#endif
        }

        /// <summary>
        /// Called when a cloud anchor is not saved successfully.
        /// </summary>
        /// <param name="exception">The exception.</param>
        protected virtual void OnSaveCloudAnchorFailed(Exception exception)
        {
            // we will block the next step to show the exception message in the UI.
            isErrorActive = true;
            Debug.LogException(exception);
            Debug.Log("Failed to save anchor " + exception.ToString());

            UnityDispatcher.InvokeOnAppThread(() => this.feedbackBox.text = string.Format("Error: {0}", exception.ToString()));
        }

        /// <summary>
        /// Called when a cloud anchor is saved successfully.
        /// </summary>
        protected virtual Task OnSaveCloudAnchorSuccessfulAsync()
        {
            // To be overridden.
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when a select interaction occurs.
        /// </summary>
        /// <remarks>Currently only called for HoloLens.</remarks>
        protected override void OnSelectInteraction()
        {
#if WINDOWS_UWP || UNITY_WSA
            if(enableAdvancingOnSelect)
            {
                // On HoloLens, we just advance the demo.
                UnityDispatcher.InvokeOnAppThread(() => advanceDemoTask = AdvanceDemoAsync());
            }
#endif

            base.OnSelectInteraction();
        }

        /// <summary>
        /// Called when a touch object interaction occurs.
        /// </summary>
        /// <param name="hitPoint">The position.</param>
        /// <param name="target">The target.</param>
        protected override void OnSelectObjectInteraction(Vector3 hitPoint, object target)
        {
            if (IsPlacingObject())
            {
                Quaternion rotation = Quaternion.AngleAxis(0, Vector3.up);

                SpawnOrMoveCurrentAnchoredObject(hitPoint, rotation);
            }
        }

        /// <summary>
        /// Called when a touch interaction occurs.
        /// </summary>
        /// <param name="touch">The touch.</param>
        protected override void OnTouchInteraction(Touch touch)
        {
            if (IsPlacingObject())
            {
                base.OnTouchInteraction(touch);
            }
        }

        /// <summary>
        /// Saves the current object anchor to the cloud.
        /// </summary>
        protected virtual async Task SaveCurrentObjectAnchorToCloudAsync()
        {
            // Get the cloud-native anchor behavior
            CloudNativeAnchor cna = spawnedObject.GetComponent<CloudNativeAnchor>();
            Debug.Log("osama 🙍‍♀️🙍‍♀️🙍‍♀️ : CloudNativeAnchor cna: " + cna.ToString());
            // If the cloud portion of the anchor hasn't been created yet, create it
            if (cna.CloudAnchor == null) { cna.NativeToCloud(); }
            Debug.Log("osama 🙍‍♀️🙍‍♀️🙍‍♀️ : CloudNativeAnchor cna2: " + cna.ToString());
            
            // Get the cloud portion of the anchor
            CloudSpatialAnchor cloudAnchor = cna.CloudAnchor;

            Debug.Log("osama 🙍‍♀️🙍‍♀️🙍‍♀️ : CloudSpatialAnchor cloudAnchor.Identifier : " + cloudAnchor.Identifier);
            Debug.Log("osama 🙍‍♀️🙍‍♀️🙍‍♀️ : CloudSpatialAnchor cloudAnchor.Expiration : " + cloudAnchor.Expiration);
            Debug.Log("osama 🙍‍♀️🙍‍♀️🙍‍♀️ : CloudSpatialAnchor cloudAnchor toString : " + cloudAnchor.ToString());
            
            // In this sample app we delete the cloud anchor explicitly, but here we show how to set an anchor to expire automatically
            cloudAnchor.Expiration = DateTimeOffset.Now.AddDays(100);
            Debug.Log("osama 🙍‍♀️🙍‍♀️🙍‍♀️ : CloudSpatialAnchor cloudAnchor.Expiration2 : " + cloudAnchor.Expiration);

            while (!CloudManager.IsReadyForCreate)
            {
                Debug.Log("osama 🙍‍♀️🙍‍♀️🙍‍♀️ !CloudManager.IsReadyForCreate : We will wait for 330 3️⃣3️⃣3️⃣" );
                Debug.Log("osama 🙍‍♀️🙍‍♀️🙍‍♀️ We are not ready yet" );
                await Task.Delay(330);
                float createProgress = CloudManager.SessionStatus.RecommendedForCreateProgress;
                feedbackBox.text = $"Move your device to capture more environment data: {createProgress:0%}"; //Osama notice this

                //Debug.Log("Progress: " + createProgress);

                XRUXPicker.Instance.SetProgress(0);
                XRUXPicker.Instance.ShowProgressBar();

                int value = (int)(100f * createProgress);
                // Debug.Log(string.Format("Upload progress: {0}%", value));
                XRUXPicker.Instance.SetProgress(value);

                if (value >= 100)
                    XRUXPicker.Instance.HideProgressBar();
            }
            Debug.Log("osama We are ready for create 🍾🍾🍾🍾🍾" );

            bool success = false;

            feedbackBox.text = "Saving...";

            try
            {
                // Actually save
                Debug.Log("osama 🙍‍♀️🙍‍♀️🙍‍♀️ : tying to save");
                Debug.Log("osama 🙍‍♀️🙍‍♀️🙍‍♀️ : CloudSpatialAnchor cloudAnchor.Identifier : " + cloudAnchor.Identifier);
                await CloudManager.CreateAnchorAsync(cloudAnchor);
                Debug.Log("osama passed CreateAnchorAsync await 🍾🍾🍾🍾");

                // Store
                currentCloudAnchor = cloudAnchor;

                // Success?
                success = currentCloudAnchor != null;
                Debug.Log("osama 🙍‍♀️🙍‍♀️🙍‍♀️ success = currentCloudAnchor != null: (sucess) = " + success);


                if (success && !isErrorActive)
                {
                    // Await override, which may perform additional tasks
                    // such as storing the key in the AnchorExchanger
                    Debug.Log("osama success && !isErrorActive 🍾🍾🍾🍾");

                    await OnSaveCloudAnchorSuccessfulAsync();
                }
                else
                {
                    Debug.Log("osama calling OnSaveCloudAnchorFailed ❌:: Failed to save, but no exception was thrown");
                    OnSaveCloudAnchorFailed(new Exception("Failed to save, but no exception was thrown."));
                }
            }
            catch (Exception ex)
            {
                Debug.Log("osama calling OnSaveCloudAnchorFailed ❌// exeption:: "+ex.Message);
                OnSaveCloudAnchorFailed(ex);
            }
        }

        /// <summary>
        /// Spawns a new anchored object.
        /// </summary>
        /// <param name="worldPos">The world position.</param>
        /// <param name="worldRot">The world rotation.</param>
        /// <returns><see cref="GameObject"/>.</returns>
        protected virtual GameObject SpawnNewAnchoredObject(Vector3 worldPos, Quaternion worldRot)
        {

            if (ARSpace == null)
            {
                // Create the prefab
                ARSpace = GameObject.Instantiate(AnchoredObjectPrefab, worldPos, worldRot);
            }
            else
            {
                ARSpace.transform.position = worldPos;
                ARSpace.transform.rotation = worldRot;
            }

            Debug.Log(ARSpace.transform.position.ToString() + " ******" + worldPos.ToString());
            // Attach a cloud-native anchor behavior to help keep cloud
            // and native anchors in sync.
            ARSpace.AddComponent<CloudNativeAnchor>();

            // Return created object
            return ARSpace;
        }

        /// <summary>
        /// Spawns a new object.
        /// </summary>
        /// <param name="worldPos">The world position.</param>
        /// <param name="worldRot">The world rotation.</param>
        /// <param name="cloudSpatialAnchor">The cloud spatial anchor.</param>
        /// <returns><see cref="GameObject"/>.</returns>
        protected virtual GameObject SpawnNewAnchoredObject(Vector3 worldPos, Quaternion worldRot, CloudSpatialAnchor cloudSpatialAnchor)
        {
            // Create the object like usual
            GameObject newGameObject = SpawnNewAnchoredObject(worldPos, worldRot);

            // If a cloud anchor is passed, apply it to the native anchor
            if (cloudSpatialAnchor != null)
            {
                CloudNativeAnchor cloudNativeAnchor = newGameObject.GetComponent<CloudNativeAnchor>();
                cloudNativeAnchor.CloudToNative(cloudSpatialAnchor);
            }

            // Return newly created object
            return newGameObject;
        }

        /// <summary>
        /// Spawns a new anchored object and makes it the current object or moves the
        /// current anchored object if one exists.
        /// </summary>
        /// <param name="worldPos">The world position.</param>
        /// <param name="worldRot">The world rotation.</param>
        protected virtual void SpawnOrMoveCurrentAnchoredObject(Vector3 worldPos, Quaternion worldRot)
        {
            // Create the object if we need to, and attach the platform appropriate
            // Anchor behavior to the spawned object
            if (spawnedObject == null)
            {
                // Use factory method to create

                spawnedObject = SpawnNewAnchoredObject(worldPos, worldRot, currentCloudAnchor);
            }
            else
            {
                // Use factory method to move

                MoveAnchoredObject(spawnedObject, worldPos, worldRot, currentCloudAnchor);
            }
        }

        private void CloudManager_AnchorLocated(object sender, AnchorLocatedEventArgs args)
        {
            Debug.LogFormat("Anchor recognized as a possible anchor {0} {1}", args.Identifier, args.Status);
            if (args.Status == LocateAnchorStatus.Located)
            {
                OnCloudAnchorLocated(args);
            }
        }

        private void CloudManager_LocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs args)
        {
            OnCloudLocateAnchorsCompleted(args);
        }

        private void CloudManager_SessionUpdated(object sender, SessionUpdatedEventArgs args)
        {
            OnCloudSessionUpdated();
        }

        private void CloudManager_Error(object sender, SessionErrorEventArgs args)
        {
            isErrorActive = true;
            Debug.Log(args.ErrorMessage);

            UnityDispatcher.InvokeOnAppThread(() => this.feedbackBox.text = string.Format("Error: {0}", args.ErrorMessage));
        }

        private void CloudManager_LogDebug(object sender, OnLogDebugEventArgs args)
        {
            Debug.Log(args.Message);
        }

        protected struct DemoStepParams
        {
            public string StepMessage { get; set; }
        }

        #region Public Properties
        /// <summary>
        /// Gets the prefab used to represent an anchored object.
        /// </summary>
        public GameObject AnchoredObjectPrefab { get { return anchoredObjectPrefab; } }

        /// <summary>
        /// Gets the <see cref="SpatialAnchorManager"/> instance used by this demo.
        /// </summary>
        public SpatialAnchorManager CloudManager { get { return cloudManager; } }
        #endregion // Public Properties
    }
}
