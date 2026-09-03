package com.vpmedia.vbot.wrapper

import android.content.Context
import android.media.AudioManager
import android.os.Handler
import android.os.Looper
import android.util.Log
import androidx.annotation.Keep
import com.google.firebase.messaging.FirebaseMessaging
import com.vpmedia.sdkvbot.client.ClientListener
import com.vpmedia.sdkvbot.client.VBotClient
import com.vpmedia.sdkvbot.client.VBotCompletion
import com.vpmedia.sdkvbot.client.VBotConfig
import com.vpmedia.sdkvbot.client.VBotEnvironment
import com.vpmedia.sdkvbot.en.AccountRegistrationState
import com.vpmedia.sdkvbot.en.CallState
import com.vpmedia.sdkvbot.en.VBotCallEndParty
import com.vpmedia.sdkvbot.en.VBotEndCallReason
import org.json.JSONArray
import org.json.JSONObject

@Keep
class VBotAndroidWrapper private constructor(private val context: Context) {

    private val mainHandler = Handler(Looper.getMainLooper())
    private var client: VBotClient? = null
    private var listener: VBotWrapperListener? = null
    private var targetActivityClass: Class<*>? = null

    var currentCallName: String = ""
        private set
    var currentCallState: String = "none"
        private set
    var isIncoming: Boolean = false
        private set
    var isMute: Boolean = false
        private set
    var isSpeaker: Boolean = false
        private set
    var onHold: Boolean = false
        private set
    var firebaseToken: String = ""
        private set

    companion object {
        private const val TAG = "VBotAndroidWrapper"

        @Volatile
        private var instance: VBotAndroidWrapper? = null

        @JvmStatic
        fun getInstance(context: Context): VBotAndroidWrapper {
            return instance ?: synchronized(this) {
                instance ?: VBotAndroidWrapper(context.applicationContext).also { instance = it }
            }
        }
    }

    init {
        initSDK("PRODUCTION", null)
        fetchFirebaseToken()
    }

    fun setListener(listener: VBotWrapperListener?) {
        this.listener = listener
    }

    fun setTargetActivityClass(activityClass: Class<*>?) {
        this.targetActivityClass = activityClass
    }

    fun setFirebaseToken(token: String) {
        this.firebaseToken = token
    }

    private fun fetchFirebaseToken() {
        try {
            FirebaseMessaging.getInstance().token.addOnCompleteListener { task ->
                if (task.isSuccessful && task.result != null) {
                    firebaseToken = task.result
                    Log.d(TAG, "Firebase token fetched: $firebaseToken")
                }
            }
        } catch (e: Exception) {
            Log.w(TAG, "Could not fetch Firebase token automatically: ${e.message}")
        }
    }

    // MARK: - SDK Init

    fun initSDK(environment: String = "PRODUCTION", customBaseUrl: String? = null) {
        val env = when (environment.uppercase()) {
            "STAGING" -> VBotEnvironment.STAGING
            "SANDBOX" -> VBotEnvironment.SANDBOX
            else -> VBotEnvironment.PRODUCTION
        }

        val config = if (!customBaseUrl.isNullOrBlank()) {
            VBotConfig(env, customBaseUrl)
        } else {
            VBotConfig(env)
        }

        if (client == null) {
            client = VBotClient(context).apply {
                setup(config)
                addListener(sdkListener)
            }
        } else {
            client?.setup(config)
        }
    }

    // MARK: - Session Connection

    fun isUserConnected(): Boolean {
        return client?.isUserConnected ?: false
    }

    fun getUserDisplayName(): String? {
        return client?.userDisplayName
    }

    fun connect(
        token: String,
        environment: String? = null,
        customBaseUrl: String? = null,
        callback: VBotCallback? = null
    ) {
        if (!environment.isNullOrBlank() || !customBaseUrl.isNullOrBlank()) {
            initSDK(environment ?: "PRODUCTION", customBaseUrl)
        }

        val tokenFcm = firebaseToken
        client?.connect(token, tokenFcm, object : VBotCompletion {
            override fun onResult(result: String?) {
                mainHandler.post {
                    callback?.onSuccess(result)
                }
            }

            override fun onError(code: String?, message: String?) {
                mainHandler.post {
                    callback?.onError(code ?: "-1", message ?: "Unknown connection error")
                }
            }
        })
    }

    fun disconnect(callback: VBotCallback? = null) {
        client?.disconnect(object : VBotCompletion {
            override fun onResult(result: String?) {
                mainHandler.post {
                    callback?.onSuccess(result)
                }
            }

            override fun onError(code: String?, message: String?) {
                mainHandler.post {
                    callback?.onError(code ?: "-1", message ?: "Disconnect failed")
                }
            }
        })
    }

    // MARK: - Calling

    fun startOutgoingCall(
        displayName: String,
        phoneNumber: String,
        hotline: String,
        externalCallId: String? = null,
        callback: VBotCallback? = null
    ) {
        isIncoming = false
        currentCallName = if (displayName.isNotBlank()) displayName else phoneNumber

        client?.startOutgoingCall(
            displayName,
            phoneNumber,
            hotline,
            externalCallId,
            object : VBotCompletion {
                override fun onResult(result: String?) {
                    mainHandler.post {
                        callback?.onSuccess(result ?: phoneNumber)
                    }
                }

                override fun onError(code: String?, message: String?) {
                    mainHandler.post {
                        callback?.onError(code ?: "-1", message ?: "Call initiation failed")
                    }
                }
            }
        )
    }

    fun answer() {
        client?.answer()
    }

    fun hangup(callback: VBotCallback? = null) {
        client?.hangup(object : VBotCompletion {
            override fun onResult(result: String?) {
                OngoingCallNotification.cancel(context)
                mainHandler.post {
                    callback?.onSuccess(result)
                }
            }

            override fun onError(code: String?, message: String?) {
                OngoingCallNotification.cancel(context)
                mainHandler.post {
                    callback?.onError(code ?: "-1", message ?: "Hangup error")
                }
            }
        })
    }

    fun setMute(muted: Boolean) {
        isMute = muted
        client?.mute(muted)
        notifyState()
    }

    fun toggleMute() {
        setMute(!isMute)
    }

    fun setSpeaker(speaker: Boolean) {
        isSpeaker = speaker
        val audioManager = context.getSystemService(Context.AUDIO_SERVICE) as? AudioManager
        audioManager?.let {
            it.isSpeakerphoneOn = speaker
            it.mode = if (speaker) AudioManager.MODE_NORMAL else AudioManager.MODE_IN_COMMUNICATION
        }
        notifyState()
    }

    fun toggleSpeaker() {
        setSpeaker(!isSpeaker)
    }

    fun sendDTMF(digit: String) {
        client?.sendDTMF(digit)
    }

    // MARK: - Hotlines

    fun getHotlinesJson(callback: VBotCallback) {
        client?.getHotlines(object : VBotCompletion {
            override fun onResult(result: String?) {
                mainHandler.post {
                    callback.onSuccess(result ?: "[]")
                }
            }

            override fun onError(code: String?, message: String?) {
                mainHandler.post {
                    callback.onError(code ?: "-1", message ?: "Failed to load hotlines")
                }
            }
        })
    }

    // MARK: - Incoming Push Handler

    fun handleIncomingPush(dataMap: Map<String, String>) {
        if (dataMap.containsKey("transId")) {
            val caller = dataMap["name"] ?: ""
            if (caller.isNotEmpty()) {
                currentCallName = caller
                isIncoming = true
            }
            currentCallState = "incoming"

            client?.notificationCall(HashMap(dataMap))
            notifyState()
        }
    }

    private fun notifyState() {
        mainHandler.post {
            listener?.onCallStateChanged(
                currentCallState,
                currentCallName,
                isIncoming,
                isMute,
                isSpeaker
            )
        }
    }

    // MARK: - SDK Listener

    private val sdkListener = object : ClientListener() {
        override fun onUserConnected(displayName: String) {
            mainHandler.post {
                listener?.onUserConnected(displayName)
            }
        }

        override fun onAccountRegistrationState(status: AccountRegistrationState, reason: String) {
            Log.d(TAG, "onAccountRegistrationState: status=$status, reason=$reason")
        }

        override fun onCallState(state: CallState) {
            Log.d(TAG, "onCallState: $state")
            when (state) {
                CallState.Null -> {
                    isIncoming = false
                    currentCallState = "none"
                    OngoingCallNotification.cancel(context)
                }
                CallState.Calling, CallState.Early -> {
                    isIncoming = false
                    currentCallState = "calling"
                }
                CallState.Incoming -> {
                    isIncoming = true
                    currentCallState = "incoming"
                }
                CallState.Connecting -> {
                    currentCallState = "connecting"
                }
                CallState.Confirmed -> {
                    currentCallState = "confirmed"
                    OngoingCallNotification.show(context, currentCallName, targetActivityClass)
                }
                CallState.Disconnected -> {
                    currentCallState = "disconnected"
                    OngoingCallNotification.cancel(context)
                }
            }
            notifyState()
        }

        override fun onCallEnded(reason: VBotEndCallReason, endedBy: VBotCallEndParty) {
            val reasonStr = reason.name
            val endedByStr = endedBy.name
            OngoingCallNotification.cancel(context)
            mainHandler.post {
                listener?.onCallEnded(reasonStr, endedByStr)
            }
        }

        override fun onCallMuteStateDidChange(muted: Boolean) {
            isMute = muted
            notifyState()
        }

        override fun onCallHoldStateDidChange(hold: Boolean) {
            onHold = hold
            notifyState()
        }
    }
}
