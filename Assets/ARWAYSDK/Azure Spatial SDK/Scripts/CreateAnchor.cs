using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using Microsoft.Azure.SpatialAnchors.Unity.Examples;
using Arway;

public class CreateAnchor : DemoScriptBase
{
    internal enum AppState
    {
        DemoStepCreateSession = 0,
        DemoStepStopSession,
        DemoStepBusy
    }

    private readonly Dictionary<AppState, DemoStepParams> stateParams = new Dictionary<AppState, DemoStepParams>
        {
            { AppState.DemoStepCreateSession,new DemoStepParams() { StepMessage = "Next: Create Azure Spatial Anchors Session" }},
            { AppState.DemoStepStopSession,new DemoStepParams() { StepMessage = "Next: Stop Azure Spatial Anchors Session" }},
            { AppState.DemoStepBusy,new DemoStepParams() { StepMessage = "Processing..." }}
        };

    private AppState _currentAppState = AppState.DemoStepCreateSession;

    AppState currentAppState
    {
        get
        {
            Debug.Log("osama XX currentAppState GET is " + _currentAppState);
            return _currentAppState;
        }
        set
        {
            if (_currentAppState != value)
            {
                Debug.LogFormat("State from {0} to {1}", _currentAppState, value);
                _currentAppState = value;

                if (!isErrorActive)
                {
                    feedbackBox.text = stateParams[_currentAppState].StepMessage;
                }
            }
            Debug.Log("osama XX currentAppState SET is " + _currentAppState);
        }
    }

    [HideInInspector]
    public static string currentAnchorId = "";

    public static Vector3 ARCameraPos = Vector3.zero;
    public static Quaternion ARCameraRot = Quaternion.identity;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before any
    /// of the Update methods are called the first time.
    /// </summary>
    public override void Start()
    {
        base.Start();

        if (!SanityCheckAccessConfiguration())
        {
            return;
        }
        feedbackBox.text = stateParams[currentAppState].StepMessage;

        Debug.Log("osama XX Azure Spatial Anchors script started");
        Debug.Log("Start: " + currentAnchorId);
    }

    protected override void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
    {
        base.OnCloudAnchorLocated(args);

        if (args.Status == LocateAnchorStatus.Located)
        {
            currentCloudAnchor = args.Anchor;
            Debug.Log("osama 1 currentCloudAnchor id is " + currentCloudAnchor.Identifier);

            UnityDispatcher.InvokeOnAppThread(() =>
            {
                Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
                anchorPose = currentCloudAnchor.GetPose();
                Debug.Log("osama 2 currentCloudAnchor pose is " + anchorPose);
#endif
                // HoloLens: The position will be set based on the unityARUserAnchor that was located.
                SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);
            });
        }
    }

    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    public override void Update()
    {
        base.Update();
    }

    protected override bool IsPlacingObject()
    {
        Debug.Log("osama xx IsPlacingObject id is " + currentCloudAnchor.Identifier);
        return currentAppState == AppState.DemoStepCreateSession;
    }

    protected override async Task OnSaveCloudAnchorSuccessfulAsync()
    {
        await base.OnSaveCloudAnchorSuccessfulAsync();

        Debug.Log("Anchor created, yay!");

        currentAnchorId = currentCloudAnchor.Identifier;
        Debug.Log("osama 3 currentCloudAnchor id is " + currentCloudAnchor.Identifier);

        PlayerPrefs.SetString("CURR_ANCHOR_ID", currentAnchorId);

        Debug.Log("yay ANCHOR ID :- " + currentAnchorId);
        NotificationManager.Instance.GenerateSuccess("ANCHOR ID :- " + currentAnchorId);
        // Sanity check that the object is still where we expect
        Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
        anchorPose = currentCloudAnchor.GetPose();
#endif

        Debug.Log("osama 4 currentCloudAnchor id is " + currentCloudAnchor.Identifier);

        // HoloLens: The position will be set based on the unityARUserAnchor that was located.

        SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

        currentAppState = AppState.DemoStepStopSession;
    }

    protected override void OnSaveCloudAnchorFailed(Exception exception)
    {
        Debug.Log("osama xx OnSaveCloudAnchorFailed:  " + currentAnchorId);
        base.OnSaveCloudAnchorFailed(exception);

        currentAnchorId = string.Empty;
    }

    public async override Task AdvanceDemoAsync()
    {
        switch (currentAppState)
        {
            case AppState.DemoStepCreateSession:
                currentAppState = AppState.DemoStepBusy;

                if (CloudManager.Session == null)
                {
                    await CloudManager.CreateSessionAsync();
                }
                Debug.Log("osama xx AdvanceDemoAsync - create session: " + currentAnchorId);
                currentAnchorId = "";
                currentCloudAnchor = null;

                await CloudManager.StartSessionAsync();
                Debug.Log("osama xx CloudManager - after await CloudManager session status: " + CloudManager.SessionStatus.ReadyForCreateProgress);
                Debug.Log("osama xx CloudManager - after await CloudManager session status: " + CloudManager.SessionStatus);
                Debug.Log("osama xx CloudManager - after await CloudManager session status: " + CloudManager.SessionStatus.ToString());
                Debug.Log("osama xx CloudManager - after await CloudManager account id: " + CloudManager.SpatialAnchorsAccountId);
                Debug.Log("osama xx CloudManager - after await CloudManager account key: " + CloudManager.SpatialAnchorsAccountKey);
                Debug.Log("osama xx CloudManager - after await CloudManager account domain: " + CloudManager.SpatialAnchorsAccountDomain);
                Debug.Log("osama xx CloudManager - after await CloudManager mode: " + CloudManager.AuthenticationMode);
                Debug.Log("osama xx CloudManager - after await CloudManager clientId: " + CloudManager.ClientId);
                Debug.Log("osama xx CloudManager - after await CloudManager didAwake: " + CloudManager.didAwake);
                Debug.Log("osama xx CloudManager - after await CloudManager didStart: " + CloudManager.didStart);

                ARCameraPos = Camera.main.transform.position;
                ARCameraRot = Quaternion.identity;
                Debug.Log("osama xx ARCameraPos: " + ARCameraPos);
                Debug.Log("osama xx ARCameraRot: " + ARCameraRot);


                // Create Anchor At Camera Transform
                SpawnOrMoveCurrentAnchoredObject(ARCameraPos, ARCameraRot);

                currentAppState = AppState.DemoStepStopSession;
                break;
            case AppState.DemoStepStopSession:
                currentAppState = AppState.DemoStepBusy;
                Debug.Log("osama xx stop session: " + currentAnchorId);
                if (spawnedObject != null)
                {
                    Debug.Log("osama xx stop spawnedObject!= null: " + spawnedObject);
                    await SaveCurrentObjectAnchorToCloudAsync();
                    Debug.Log("osama zzz done await: " + spawnedObject);
                }
                else
                {
                    
                    Debug.Log("osama xx stop spawnedObject == null: " + spawnedObject);
                    SpawnOrMoveCurrentAnchoredObject(ARCameraPos, ARCameraRot);
                    Debug.Log("osama xx stop spawnedObject == null: " + spawnedObject);
                    await SaveCurrentObjectAnchorToCloudAsync();
                    Debug.Log("osama zzz done await: " + spawnedObject);
                }

                CloudManager.StopSession();
                CleanupSpawnedObjects();
                await CloudManager.ResetSessionAsync();

                currentAppState = AppState.DemoStepCreateSession;

                break;
            case AppState.DemoStepBusy:
                Debug.Log("DemoStepBusy: " + currentAnchorId);
                break;
            default:
                Debug.Log("Shouldn't get here for app state " + currentAppState.ToString());
                break;
        }
    }

    public static string getCurrentAnchorId()
    {
        return currentAnchorId;
    }
}
