package com.vpmedia.vbot.wrapper

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import androidx.annotation.Keep

@Keep
class HangupReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        VBotAndroidWrapper.getInstance(context).hangup(null)
    }
}
