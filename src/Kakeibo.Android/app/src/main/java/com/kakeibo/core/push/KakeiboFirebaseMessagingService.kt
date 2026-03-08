package com.kakeibo.core.push

import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import timber.log.Timber

/**
 * Handles incoming Firebase Cloud Messaging messages.
 *
 * When the app is in the foreground, [onMessageReceived] is called and the notification
 * must be displayed manually via NotificationCompat.
 * When the app is in the background, FCM displays the notification automatically.
 *
 * On new FCM token, [onNewToken] must send the token to the Kakeibo API so the server
 * can target this device for push notifications.
 */
class KakeiboFirebaseMessagingService : FirebaseMessagingService() {

    override fun onNewToken(token: String) {
        super.onNewToken(token)
        Timber.d("FCM new token: $token")
        // TODO: send token to API via PushSubscriptionRepository
    }

    override fun onMessageReceived(message: RemoteMessage) {
        super.onMessageReceived(message)
        Timber.d("FCM message received: ${message.notification?.title}")
        // TODO: build and display NotificationCompat notification
        // Deep link via message.data["route"] → navController.handleDeepLink(...)
    }
}
