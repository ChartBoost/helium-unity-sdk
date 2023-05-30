package com.chartboost.mediation.unity

import android.util.Log
import com.chartboost.heliumsdk.domain.ChartboostMediationAdException
import com.chartboost.heliumsdk.domain.ChartboostMediationError
import org.json.JSONException
import org.json.JSONObject

@Deprecated("EventProcessor utilizes deprecated APIs and will be removed in the future")
object EventProcessor {
    private val TAG = EventProcessor::class.java.simpleName

    @JvmStatic
    fun serializeEvent(placementName: String, eventConsumer: EventConsumer<String>)
        = eventConsumer.accept(placementName)

    @JvmStatic
    fun serializeLoadEvent(
        placementName: String, loadId: String?, data: Map<String, String>?, error: ChartboostMediationError?,
        loadConsumer: LoadEventConsumer<String, String, String, String, Double, String>) {
        val errorMessage = error?.toString() ?: ""
        var partnerId = ""
        var auctionId = ""
        var price =  0.0
        val adLoadId = loadId ?: ""

        data?.let { winningBidInfo ->
            partnerId = winningBidInfo["partner_id"] ?: ""
            auctionId = winningBidInfo["auction-id"] ?: ""
            price = try {
                winningBidInfo["price"]?.toDouble() ?: 0.0
            } catch (e: NumberFormatException) {
                Log.d(TAG, "HeliumBidEvent failed to serialize price, defaulting to 0.0", e)
                0.0
            }
        }

        loadConsumer.accept(placementName, adLoadId, auctionId, partnerId, price, errorMessage)
    }

    @JvmStatic
    fun serializeEventWithError(placementName: String, error: ChartboostMediationError?, eventConsumer: EventWithErrorConsumer<String, String>)
        = eventConsumer.accept(placementName,error?.toString() ?: "")

    @JvmStatic
    fun serializeEventWithException(placementName: String, error: ChartboostMediationAdException?, eventConsumer: EventWithErrorConsumer<String, String>)
        = eventConsumer.accept(placementName,error?.toString() ?: "")

    @JvmStatic
    fun serializeLoadEvent(placementName: String, loadId: String, data: Map<String, String>, error: ChartboostMediationAdException?,
                           loadConsumer: LoadEventConsumer<String, String, String, String, Double, String>) {
        val errorMessage = error?.toString() ?: ""

        val partnerId = data["partner_id"] ?: ""
        val auctionId = data["auction-id"] ?: ""
        val price = try {
            data["price"]?.toDouble() ?: 0.0
        } catch (e: NumberFormatException) {
            Log.d(TAG, "HeliumBidEvent failed to serialize price, defaulting to 0.0", e)
            0.0
        }

        loadConsumer.accept(placementName, loadId, auctionId, partnerId, price, errorMessage)
    }

    fun interface EventConsumer<PlacementName>{
        fun accept(placementName: PlacementName)
    }

    fun interface EventWithErrorConsumer<PlacementName, ErrorMessage> {
        fun accept(placementName: PlacementName, errorMessage: ErrorMessage)
    }

    fun interface LoadEventConsumer<PlacementName, LoadId, AuctionId, PartnerId, Price, Error> {
        fun accept(placementName: PlacementName, loadId: LoadId, auctionId: AuctionId, partnerId: PartnerId, price: Price, error: Error)
    }
}
