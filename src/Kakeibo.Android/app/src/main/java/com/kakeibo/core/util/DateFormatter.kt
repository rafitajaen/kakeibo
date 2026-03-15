package com.kakeibo.core.util

import kotlinx.datetime.Clock
import kotlinx.datetime.Instant
import kotlinx.datetime.LocalDate
import kotlinx.datetime.TimeZone
import kotlinx.datetime.toJavaLocalDate
import kotlinx.datetime.toLocalDateTime
import kotlinx.datetime.todayIn
import java.time.format.DateTimeFormatter
import java.time.format.FormatStyle

/**
 * Date/time formatting helpers for Kakeibo.
 * Uses kotlinx-datetime for types and Java formatters only at the display layer.
 */
object DateFormatter {

    /** Parses an ISO-8601 string to [Instant]. */
    fun parseInstant(iso: String): Instant = Instant.parse(iso)

    /** Converts an [Instant] to [LocalDate] in the device's current timezone. */
    fun toLocalDate(instant: Instant): LocalDate =
        instant.toLocalDateTime(TimeZone.currentSystemDefault()).date

    /** Returns today's [LocalDate] in the device's timezone. */
    fun today(): LocalDate = Clock.System.todayIn(TimeZone.currentSystemDefault())

    /**
     * Formats a [LocalDate] as a locale-aware medium date string.
     * Example: "Mar 14, 2026"
     */
    fun formatShort(date: LocalDate): String {
        return DateTimeFormatter.ofLocalizedDate(FormatStyle.MEDIUM)
            .format(date.toJavaLocalDate())
    }

    /**
     * Formats a [LocalDate] with day name.
     * Example: "Saturday, Mar 14"
     */
    fun formatDayName(date: LocalDate): String {
        return DateTimeFormatter.ofPattern("EEEE, MMM d")
            .format(date.toJavaLocalDate())
    }

    /**
     * Formats a [LocalDate] as month + year.
     * Example: "March 2026"
     */
    fun formatMonthYear(date: LocalDate): String {
        return DateTimeFormatter.ofPattern("MMMM yyyy")
            .format(date.toJavaLocalDate())
    }

    /** Formats a [LocalDate] for group headers: "Today", "Yesterday", or "Fri, Mar 14". */
    fun formatGroupHeader(date: LocalDate): String {
        val javaToday = today().toJavaLocalDate()
        val javaDate = date.toJavaLocalDate()
        return when {
            javaDate.isEqual(javaToday) -> "Today"
            javaDate.isEqual(javaToday.minusDays(1)) -> "Yesterday"
            else -> DateTimeFormatter.ofPattern("EEE, MMM d").format(javaDate)
        }
    }

    /** Returns an ISO-8601 date string suitable for the API (e.g. "2026-03-14"). */
    fun toApiString(date: LocalDate): String =
        "${date.year}-${date.monthNumber.toString().padStart(2, '0')}-" +
            date.dayOfMonth.toString().padStart(2, '0')
}
