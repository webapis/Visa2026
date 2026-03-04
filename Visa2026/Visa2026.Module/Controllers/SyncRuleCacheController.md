# Controller: SyncRuleCacheController

## 1. Overview

The `SyncRuleCacheController` is a background system controller responsible for maintaining the integrity of the in-memory cache used by the `CrossObjectSyncHelper`. Its sole purpose is to detect when a `SyncRule` is created, modified, or deleted, and to invalidate the cache so that the changes are picked up by the application immediately.

## 2. The Problem Solved

The `CrossObjectSyncHelper` caches `SyncRule` definitions in a static dictionary (`_ruleCache`) to maximize performance by avoiding database queries on every object save. However, this creates a new problem: if an administrator changes a rule in the UI, the in-memory cache becomes stale and the application continues to execute the old, cached version of the rules.

This controller solves the cache invalidation problem automatically and reliably.

## 3. How It Works

The controller uses an event-driven approach to monitor changes within the application's data layer.

1.  **Global Scope**: It is implemented as a `WindowController`, which means it is active for the entire application window and can monitor events globally.

2.  **Object Space Subscription**: On activation, it subscribes to the `Application.ObjectSpaceCreated` event. This allows it to attach to every `IObjectSpace` (Unit of Work) that is created anywhere in the application.

3.  **Transaction Monitoring**: For each `IObjectSpace`, it subscribes to three key events:
    *   `Committing`: This event fires just *before* changes are sent to the database. The controller inspects the objects being saved (`GetObjectsToSave`) and deleted (`GetObjectsToDelete`). If it finds any object of type `SyncRule`, it sets a private boolean flag, `_cacheNeedsInvalidation`, to `true`.
    *   `Committed`: This event fires *after* the database transaction has successfully completed. The controller checks if the `_cacheNeedsInvalidation` flag is `true`. If it is, it calls `CrossObjectSyncHelper.InvalidateCache()` to clear the stale cache and then resets the flag to `false`.
    *   `Disposed`: Cleans up the event handlers to prevent memory leaks when an `IObjectSpace` is destroyed.

## 4. Benefits of this Approach

*   **Reliability**: The cache is invalidated regardless of how the `SyncRule` was changed (e.g., via a Detail View, an inline edit in a ListView, or programmatically).
*   **Transactional Safety**: By waiting for the `Committed` event, the controller ensures the cache is only cleared if the database update was successful. This prevents a state where the cache is empty but the database changes failed to save.
*   **Efficiency**: The check for `SyncRule` objects is very fast and adds negligible overhead to the commit process. The cache is only invalidated when absolutely necessary.

## 5. Code Reference

*   **Class**: `Visa2026.Module.Controllers.SyncRuleCacheController`
*   **Dependencies**: `Visa2026.Module.BusinessObjects.CrossObjectSyncHelper.InvalidateCache()`