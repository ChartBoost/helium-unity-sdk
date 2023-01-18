package com.chartboost.heliumsdk.unity

import android.util.Log
import com.chartboost.heliumsdk.domain.HeliumAdException
import org.json.JSONException
import org.json.JSONObject

object HeliumEventProcessor {
    private val TAG = HeliumEventProcessor::class.java.simpleName

    @JvmStatic
    fun serializeHeliumEvent(
        placementName: String,
        error: HeliumAdException?,
        eventConsumer: HeliumEventConsumer<String, String>
    ) {
        eventConsumer.accept(placementName,error?.toString() ?: "")
    }

    @JvmStatic
    fun serializeHeliumLoadEvent(
        placementName: String,
        data: Map<String, String>,
        error: HeliumAdException?,
        eventConsumer: HeliumLoadEventConsumer<String, String, String, Double, String>
    ) {
        val partnerId = data["partner_id"] ?: ""
        val auctionId = data["auction-id"] ?: ""
        val price = try {
            data["price"]?.toDouble() ?: 0.0
        } catch (e: NumberFormatException) {
            Log.d(TAG, "HeliumBidEvent failed to serialize price, defaulting to 0.0", e)
            0.0
        }
        eventConsumer.accept(placementName, auctionId, partnerId, price, error?.toString() ?: "")
    }

    @JvmStatic
    fun serializePlacementIlrdData(placementName: String, ilrdInfo: JSONObject?): String {
        return try {
            JSONObject().apply {
                put("placementName", placementName)
                put("ilrd", ilrdInfo)
            }.toString()
        } catch (e: JSONException) {
            Log.d(TAG, "serializeError", e)
            ""
        }
    }

    fun interface HeliumEventConsumer<PlacementName, ErrorMessage> {
        fun accept(placementName: PlacementName, errorMessage: ErrorMessage)
    }

    fun interface HeliumLoadEventConsumer<PlacementName, AuctionId, PartnerId, Price, Error> {
        fun accept(placementName: PlacementName, auctionId: AuctionId, partnerId: PartnerId, price: Price, error: Error)
    }

    fun interface HeliumRewardEventConsumer<PlacementName, Reward> {
        fun accept(placementName: PlacementName, reward: Reward)
    }
}
