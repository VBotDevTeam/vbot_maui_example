import Foundation
import UIKit
import PushKit
import AVFoundation
import VBotPhoneSDK

// MARK: - Delegate Protocol (cho MAUI C# binding lắng nghe)
@objc(VBotWrapperDelegate) public protocol VBotWrapperDelegate: AnyObject {
    @objc func onCallStateChanged(_ state: String, name: String, isIncoming: Bool, isMute: Bool, onHold: Bool)
    @objc func onCallEnded(_ reason: String, endedBy: String)
    @objc func onCallMuteStateDidChange(_ muted: Bool)
    @objc func onCallStarted()
    @objc func onCallAccepted()
    @objc func onPushKitTokenReceived(_ token: String)
}

// MARK: - Main Wrapper
@objc(VBotWrapper) public class VBotWrapper: NSObject {

    @objc public static let shared = VBotWrapper()

    @objc public weak var delegate: VBotWrapperDelegate?

    // State tracking
    @objc public private(set) var currentCallName: String = ""
    @objc public private(set) var currentCallState: String = "none"
    @objc public private(set) var isIncoming: Bool = false
    @objc public private(set) var isMuted: Bool = false
    @objc public private(set) var isSpeakerOn: Bool = false

    private var voipRegistry: PKPushRegistry?
    private var cachedPushKitToken: String = ""
    private var lastCallState: VBotCallState = .null

    private override init() {
        super.init()
    }

    // MARK: - SDK Setup

    /// Khởi tạo SDK - gọi trong AppDelegate.FinishedLaunching
    @objc public func initialize(environment: String, customBaseUrl: String?) {
        let env: VBotEnvironment
        switch environment.uppercased() {
        case "STAGING": env = .staging
        case "SANDBOX": env = .sandbox
        default: env = .production
        }

        let config = VBotConfig(
            supportPopupCall: false,
            includesCallsInRecents: true,
            iconTemplateImageData: nil,
            environment: env,
            customBaseUrl: customBaseUrl
        )

        VBotPhone.sharedInstance.setup(with: config)
        VBotPhone.sharedInstance.addDelegate(self)

        // Setup PushKit
        setupPushKit()

        // Request mic permission
        checkMicrophonePermission()
    }

    /// Cập nhật config (khi connect với environment khác)
    @objc public func setConfig(environment: String, customBaseUrl: String?) {
        let env: VBotEnvironment
        switch environment.uppercased() {
        case "STAGING": env = .staging
        case "SANDBOX": env = .sandbox
        default: env = .production
        }

        let config = VBotConfig(
            environment: env,
            customBaseUrl: customBaseUrl
        )
        VBotPhone.sharedInstance.setConfig(config: config)
    }

    // MARK: - PushKit Setup

    private func setupPushKit() {
        self.voipRegistry = PKPushRegistry(queue: DispatchQueue.main)
        self.voipRegistry?.delegate = self
        self.voipRegistry?.desiredPushTypes = [.voIP]
    }

    private func checkMicrophonePermission() {
        let status = AVAudioSession.sharedInstance().recordPermission
        if status == .undetermined {
            AVAudioSession.sharedInstance().requestRecordPermission { _ in }
        }
    }

    // MARK: - Connect/Disconnect

    /// Kết nối SDK
    @objc public func connect(
        token: String,
        environment: String?,
        customBaseUrl: String?,
        completion: @escaping (NSString?, NSError?) -> Void
    ) {
        // Cập nhật cấu hình nếu có môi trường mới
        if let envStr = environment, !envStr.isEmpty {
            setConfig(environment: envStr, customBaseUrl: customBaseUrl)
        }

        let pushkitToken = VBotPhone.sharedInstance.pushKitToken ?? cachedPushKitToken
        VBotPhone.sharedInstance.connect(token: token, pushkitToken: pushkitToken) { displayName, error in
            completion(displayName as NSString?, error as NSError?)
        }
    }

    /// Ngắt kết nối SDK
    @objc public func disconnect(completion: @escaping (NSError?) -> Void) {
        VBotPhone.sharedInstance.disconnect { error in
            completion(error as NSError?)
        }
    }

    // MARK: - Calling

    /// Gọi đi
    @objc public func startOutgoingCall(
        displayName: String,
        number: String,
        hotline: String,
        externalCallId: String?,
        completion: @escaping (Bool, NSError?) -> Void
    ) {
        self.isIncoming = false
        self.currentCallName = displayName.isEmpty ? number : displayName

        VBotPhone.sharedInstance.startOutgoingCall(
            displayName: displayName,
            number: number,
            hotline: hotline,
            externalCallId: externalCallId
        ) { success, error in
            completion(success, error as NSError?)
        }
    }

    /// Nhận cuộc gọi đến từ PushKit payload
    @objc public func startIncomingCall(payload: PKPushPayload, completion: @escaping () -> Void) {
        VBotPhone.sharedInstance.startIncomingCall(payload: payload, completion: completion)
    }

    /// Kết thúc cuộc gọi
    @objc public func endCall(completion: @escaping (NSError?) -> Void) {
        VBotPhone.sharedInstance.endCall { error in
            completion(error as NSError?)
        }
    }

    /// Trả lời cuộc gọi đến
    @objc public func answerCall(completion: @escaping (NSError?) -> Void) {
        VBotPhone.sharedInstance.answerCall { error in
            completion(error as NSError?)
        }
    }

    /// Từ chối cuộc gọi đến
    @objc public func declineIncomingCall(isBusy: Bool, completion: @escaping (NSError?) -> Void) {
        VBotPhone.sharedInstance.declineIncomingCall(isBusy: isBusy) { error in
            completion(error as NSError?)
        }
    }

    /// Bật/Tắt Micro
    @objc public func muteCall() {
        VBotPhone.sharedInstance.muteCall()
    }

    /// Bật/Tắt Loa ngoài
    @objc public func onOffSpeaker() {
        VBotPhone.sharedInstance.onOffSpeaker()
    }

    /// Gửi phím DTMF
    @objc public func sendDTMF(character: String) {
        VBotPhone.sharedInstance.sendDTMF(character: character)
    }

    /// Lấy danh sách hotline
    @objc public func getHotlines(completion: @escaping (NSArray?, NSError?) -> Void) {
        VBotPhone.sharedInstance.getHotlines { hotlines, error in
            if let hotlines = hotlines {
                let list: [[String: String]] = hotlines.map { h in
                    ["name": h.name, "phoneNumber": h.phoneNumber]
                }
                completion(list as NSArray, nil)
            } else {
                completion(nil, error as NSError?)
            }
        }
    }

    /// Lấy tên hiển thị của user
    @objc public func getUserDisplayName() -> String? {
        return VBotPhone.sharedInstance.getUserDisplayName()
    }

    /// Kiểm tra trạng thái kết nối
    @objc public func isUserConnected() -> Bool {
        return VBotPhone.sharedInstance.isUserConnected()
    }

    /// PushKit token hiện tại
    @objc public var pushKitToken: String? {
        return VBotPhone.sharedInstance.pushKitToken ?? (cachedPushKitToken.isEmpty ? nil : cachedPushKitToken)
    }
}

// MARK: - VBotPhoneDelegate
extension VBotWrapper: VBotPhoneDelegate {

    public func callStateChanged(state: VBotCallState) {
        self.lastCallState = state

        let stateStr: String
        switch state {
        case .calling, .early:
            stateStr = "calling"
            isIncoming = false
        case .incoming:
            stateStr = "incoming"
            isIncoming = true
        case .connecting:
            stateStr = "connecting"
        case .confirmed:
            stateStr = "confirmed"
        default:
            stateStr = "disconnected"
        }

        NSLog("[VBotWrapper] callStateChanged: \(state.rawValue) -> '\(stateStr)', name='\(currentCallName)', incoming=\(isIncoming)")
        self.currentCallState = stateStr
        delegate?.onCallStateChanged(stateStr, name: currentCallName, isIncoming: isIncoming, isMute: isMuted, onHold: false)
    }

    public func callMuteStateDidChange(muted: Bool) {
        NSLog("[VBotWrapper] callMuteStateDidChange: \(muted)")
        self.isMuted = muted
        delegate?.onCallMuteStateDidChange(muted)
    }

    public func callEnded(reason: VBotEndCallReason) {
        NSLog("[VBotWrapper] callEnded: reason=\(reason.rawValue)")
        self.currentCallState = "disconnected"
        delegate?.onCallEnded("\(reason.rawValue)", endedBy: "unknown")
        self.currentCallName = ""
    }

    public func callEnded(reason: VBotEndCallReason, endedBy: VBotCallEndParty) {
        NSLog("[VBotWrapper] callEnded: reason=\(reason.rawValue), endedBy=\(endedBy.description)")
        self.currentCallState = "disconnected"
        delegate?.onCallEnded("\(reason.rawValue)", endedBy: endedBy.description)
        self.currentCallName = ""
    }

    public func callStarted() {
        NSLog("[VBotWrapper] callStarted")
        delegate?.onCallStarted()
    }

    public func callAccepted() {
        NSLog("[VBotWrapper] callAccepted")
        delegate?.onCallAccepted()
    }
}

// MARK: - PKPushRegistryDelegate
extension VBotWrapper: PKPushRegistryDelegate {

    /// Nhận PushKit token từ Apple
    public func pushRegistry(_ registry: PKPushRegistry, didUpdate pushCredentials: PKPushCredentials, for type: PKPushType) {
        let token = pushCredentials.token.map { String(format: "%02.2hhx", $0) }.joined()
        NSLog("[VBotWrapper] PushKit token received: \(token.prefix(20))...")
        self.cachedPushKitToken = token
        VBotPhone.sharedInstance.pushKitToken = token
        delegate?.onPushKitTokenReceived(token)
    }

    /// Nhận cuộc gọi VoIP push
    public func pushRegistry(_ registry: PKPushRegistry, didReceiveIncomingPushWith payload: PKPushPayload, for type: PKPushType, completion: @escaping () -> Void) {
        NSLog("[VBotWrapper] Incoming VoIP push received, type=\(type.rawValue)")
        NSLog("[VBotWrapper] isUserConnected=\(VBotPhone.sharedInstance.isUserConnected()), pushKitToken=\(VBotPhone.sharedInstance.pushKitToken?.prefix(20) ?? "nil")")
        if type == .voIP {
            self.isIncoming = true

            // Bóc tách tên / số điện thoại người gọi từ push payload
            var callerName = ""
            let dict = payload.dictionaryPayload

            if let customDict = dict["custom"] as? [String: Any] {
                if let name = customDict["name"] as? String, !name.isEmpty {
                    callerName = name
                } else if let caller = customDict["caller"] as? String, !caller.isEmpty {
                    callerName = caller
                } else if let projectName = customDict["project_name"] as? String, !projectName.isEmpty {
                    callerName = projectName
                }
            } else if let customStr = dict["custom"] as? String,
                      let data = customStr.data(using: .utf8),
                      let customJson = try? JSONSerialization.jsonObject(with: data) as? [String: Any] {
                if let name = customJson["name"] as? String, !name.isEmpty {
                    callerName = name
                } else if let caller = customJson["caller"] as? String, !caller.isEmpty {
                    callerName = caller
                } else if let projectName = customJson["project_name"] as? String, !projectName.isEmpty {
                    callerName = projectName
                }
            }

            if callerName.isEmpty {
                if let name = dict["name"] as? String, !name.isEmpty {
                    callerName = name
                } else if let caller = dict["caller"] as? String, !caller.isEmpty {
                    callerName = caller
                } else if let aps = dict["aps"] as? [String: Any],
                          let alert = aps["alert"] as? [String: Any],
                          let title = alert["title"] as? String, !title.isEmpty {
                    callerName = title
                }
            }

            if !callerName.isEmpty {
                NSLog("[VBotWrapper] Extracted incoming caller name: '\(callerName)'")
                self.currentCallName = callerName
            }

            VBotPhone.sharedInstance.startIncomingCall(payload: payload) {
                NSLog("[VBotWrapper] startIncomingCall completion called")
                completion()
            }
        } else {
            completion()
        }
    }

    public func pushRegistry(_ registry: PKPushRegistry, didInvalidatePushTokenFor type: PKPushType) {
        NSLog("[VBotWrapper] PushKit token invalidated")
        VBotPhone.sharedInstance.pushKitToken = nil
        self.cachedPushKitToken = ""
    }
}
