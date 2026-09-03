using System;
using Foundation;
using ObjCRuntime;
using UIKit;
using PushKit;

namespace VBot.iOS.SDK
{
    // @protocol VBotWrapperDelegate
    [Protocol(Name = "VBotWrapperDelegate"), Model]
    [BaseType(typeof(NSObject))]
    interface VBotWrapperDelegate
    {
        [Export("onCallStateChanged:name:isIncoming:isMute:onHold:")]
        void OnCallStateChanged(string state, string name, bool isIncoming, bool isMute, bool onHold);

        [Export("onCallEnded:endedBy:")]
        void OnCallEnded(string reason, string endedBy);

        [Export("onCallMuteStateDidChange:")]
        void OnCallMuteStateDidChange(bool muted);

        [Export("onCallStarted")]
        void OnCallStarted();

        [Export("onCallAccepted")]
        void OnCallAccepted();

        [Export("onPushKitTokenReceived:")]
        void OnPushKitTokenReceived(string token);
    }

    interface IVBotWrapperDelegate { }

    // @interface VBotWrapper : NSObject
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface VBotWrapper
    {
        // @property (class, readonly, strong) VBotWrapper * shared
        [Static, Export("shared")]
        VBotWrapper Shared { get; }

        // @property (nonatomic, weak) id<VBotWrapperDelegate> delegate
        [NullAllowed, Export("delegate", ArgumentSemantic.Weak)]
        IVBotWrapperDelegate Delegate { get; set; }

        // @property (nonatomic, readonly, copy) NSString * currentCallName
        [Export("currentCallName")]
        string CurrentCallName { get; }

        // @property (nonatomic, readonly, copy) NSString * currentCallState
        [Export("currentCallState")]
        string CurrentCallState { get; }

        // @property (nonatomic, readonly) BOOL isIncoming
        [Export("isIncoming")]
        bool IsIncoming { get; }

        // @property (nonatomic, readonly) BOOL isMuted
        [Export("isMuted")]
        bool IsMuted { get; }

        // @property (nonatomic, readonly) BOOL isSpeakerOn
        [Export("isSpeakerOn")]
        bool IsSpeakerOn { get; }

        // - (void)initializeWithEnvironment:(NSString *)environment customBaseUrl:(NSString * _Nullable)customBaseUrl
        [Export("initializeWithEnvironment:customBaseUrl:")]
        void Initialize(string environment, [NullAllowed] string customBaseUrl);

        // - (void)setConfigWithEnvironment:(NSString *)environment customBaseUrl:(NSString * _Nullable)customBaseUrl
        [Export("setConfigWithEnvironment:customBaseUrl:")]
        void SetConfig(string environment, [NullAllowed] string customBaseUrl);

        // - (void)connectWithToken:(NSString *)token environment:(NSString * _Nullable)environment customBaseUrl:(NSString * _Nullable)customBaseUrl completion:...
        [Export("connectWithToken:environment:customBaseUrl:completion:")]
        void Connect(string token, [NullAllowed] string environment, [NullAllowed] string customBaseUrl, Action<NSString, NSError> completion);

        // - (void)disconnectWithCompletion:...
        [Export("disconnectWithCompletion:")]
        void Disconnect(Action<NSError> completion);

        // - (void)startOutgoingCallWithDisplayName:number:hotline:externalCallId:completion:
        [Export("startOutgoingCallWithDisplayName:number:hotline:externalCallId:completion:")]
        void StartOutgoingCall(string displayName, string number, string hotline, [NullAllowed] string externalCallId, Action<bool, NSError> completion);

        // - (void)startIncomingCallWithPayload:completion:
        [Export("startIncomingCallWithPayload:completion:")]
        void StartIncomingCall(PKPushPayload payload, Action completion);

        // - (void)endCallWithCompletion:
        [Export("endCallWithCompletion:")]
        void EndCall(Action<NSError> completion);

        // - (void)muteCall
        [Export("muteCall")]
        void MuteCall();

        // - (void)onOffSpeaker
        [Export("onOffSpeaker")]
        void OnOffSpeaker();

        // - (void)getHotlinesWithCompletion:
        [Export("getHotlinesWithCompletion:")]
        void GetHotlines(Action<NSArray, NSError> completion);

        // @property (nonatomic, readonly) BOOL isUserConnected
        [Export("isUserConnected")]
        bool IsUserConnected { get; }

        // @property (nonatomic, readonly, copy) NSString * userDisplayName
        [NullAllowed, Export("userDisplayName")]
        string UserDisplayName { get; }

        // @property (nonatomic, readonly) BOOL isCallMute
        [Export("isCallMute")]
        bool IsCallMute { get; }

        // @property (nonatomic, readonly) BOOL isCallHold
        [Export("isCallHold")]
        bool IsCallHold { get; }

        // @property (nonatomic, copy) NSString * pushKitToken
        [NullAllowed, Export("pushKitToken")]
        string PushKitToken { get; set; }

        // @property (nonatomic, readonly) BOOL hasActiveCall
        [Export("hasActiveCall")]
        bool HasActiveCall { get; }
    }
}
