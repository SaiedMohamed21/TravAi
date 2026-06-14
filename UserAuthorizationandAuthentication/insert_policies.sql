BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. Safety Checks: Ensure the 3 reference policies exist exactly as expected
    IF NOT EXISTS (SELECT 1 FROM hotel_HotelPolicies WHERE HotelId = 267 AND CancellationStrategy = 'FreeAll')
        THROW 50001, 'Reference Hotel 267 (FreeAll) policy missing or changed.', 1;
    IF NOT EXISTS (SELECT 1 FROM hotel_HotelPolicies WHERE HotelId = 268 AND CancellationStrategy = 'NonRefundable')
        THROW 50002, 'Reference Hotel 268 (NonRefundable) policy missing or changed.', 1;
    IF NOT EXISTS (SELECT 1 FROM hotel_HotelPolicies WHERE HotelId = 269 AND CancellationStrategy = 'WindowBased')
        THROW 50003, 'Reference Hotel 269 (WindowBased) policy missing or changed.', 1;

    -- 2. Temporary table to hold target hotels and their assigned group
    CREATE TABLE #HotelsToProcess (
        HotelId BIGINT,
        RowNum INT,
        GroupAssigned INT
    );

    -- 3. Find missing-policy hotels and assign deterministic groups
    INSERT INTO #HotelsToProcess (HotelId, RowNum, GroupAssigned)
    SELECT 
        h.Id,
        ROW_NUMBER() OVER (ORDER BY h.Id ASC) AS RowNum,
        (ROW_NUMBER() OVER (ORDER BY h.Id ASC) % 3) + 1 AS GroupAssigned
    FROM hotel_Hotels h
    LEFT JOIN hotel_HotelPolicies p ON h.Id = p.HotelId
    WHERE p.Id IS NULL;

    DECLARE @MissingCount INT;
    SELECT @MissingCount = COUNT(*) FROM #HotelsToProcess;
    PRINT 'Hotels identified without policies: ' + CAST(@MissingCount AS VARCHAR);

    IF @MissingCount > 0
    BEGIN
        DECLARE @InsertedPolicies TABLE (
            NewPolicyId BIGINT,
            HotelId BIGINT,
            AssignedGroup INT
        );

        -- 4. Insert new policy rows using MERGE to capture source group
        MERGE INTO hotel_HotelPolicies AS target
        USING #HotelsToProcess AS source
        ON 1 = 0
        WHEN NOT MATCHED THEN
            INSERT (HotelId, ServiceChargePct, IncludeServiceCharge, IncludeVat, IncludeCityTax, CancellationStrategy, CreatedAt, UpdatedAt)
            VALUES (
                source.HotelId,
                12.00, 
                0, 
                0, 
                0, 
                CASE 
                    WHEN source.GroupAssigned = 1 THEN 'FreeAll'
                    WHEN source.GroupAssigned = 2 THEN 'NonRefundable'
                    WHEN source.GroupAssigned = 3 THEN 'WindowBased'
                END,
                GETUTCDATE(),
                GETUTCDATE()
            )
        OUTPUT inserted.Id, inserted.HotelId, source.GroupAssigned
        INTO @InsertedPolicies (NewPolicyId, HotelId, AssignedGroup);

        -- 5. Insert cancellation rules exclusively for Group 3 (WindowBased)
        INSERT INTO hotel_HotelCancellationRules (
            HotelPolicyId, FromHoursBeforeCheckIn, ToHoursBeforeCheckIn, PenaltyPct, CreatedAt, UpdatedAt
        )
        SELECT 
            ip.NewPolicyId,
            r.FromHoursBeforeCheckIn,
            r.ToHoursBeforeCheckIn,
            r.PenaltyPct,
            GETUTCDATE(),
            GETUTCDATE()
        FROM @InsertedPolicies ip
        CROSS JOIN (
            VALUES 
                (72, NULL, 0.00),
                (48, 72, 50.00),
                (NULL, 48, 100.00)
        ) AS r(FromHoursBeforeCheckIn, ToHoursBeforeCheckIn, PenaltyPct)
        WHERE ip.AssignedGroup = 3;

        PRINT 'Successfully inserted policies and rules.';
    END
    ELSE
    BEGIN
        PRINT 'No hotels missing policies. Nothing to do.';
    END

    DROP TABLE #HotelsToProcess;

    COMMIT TRANSACTION;
    PRINT 'Transaction committed successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    PRINT 'Error occurred: ' + @ErrorMessage;
    PRINT 'Transaction rolled back safely.';
END CATCH
