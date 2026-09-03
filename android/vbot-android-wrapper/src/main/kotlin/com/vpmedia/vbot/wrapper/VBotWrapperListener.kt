package com.vpmedia.vbot.wrapper

import androidx.annotation.Keep

@Keep
interface VBotWrapperListener {
    fun onCallStateChanged(state: String, callName: String, isIncoming: Boolean, isMuted: Boolean, isSpeaker: Boolean)
    fun onCallEnded(reason: String, endedBy: String)
    fun onUserConnected(displayName: String)
}

@Keep
interface VBotCallback {
    fun onSuccess(result: String?)
    fun onError(errorCode: String, errorMessage: String)
}
