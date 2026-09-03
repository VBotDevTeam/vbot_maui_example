package com.vpmedia.vbot.wrapper

import android.content.Context
import android.util.Log
import androidx.annotation.Keep
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage

@Keep
class VBotFirebaseService : FirebaseMessagingService() {

    override fun onNewToken(token: String) {
        super.onNewToken(token)
        Log.d("VBotFirebaseService", "onNewToken: $token")
        VBotAndroidWrapper.getInstance(applicationContext).setFirebaseToken(token)
    }

    override fun onMessageReceived(remoteMessage: RemoteMessage) {
        super.onMessageReceived(remoteMessage)
        val data = remoteMessage.data
        Log.d("VBotFirebaseService", "onMessageReceived: $data")
        if (data.containsKey("transId")) {
            VBotAndroidWrapper.getInstance(applicationContext).handleIncomingPush(data)
        }
    }
}
