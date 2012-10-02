/***********************************************************************************************
 * CreateGetRefsToObj
 *
 * Description:
 *	Creates stored function fnGetRefsToObj. This function retrieves refererences
 *	to an object and its owned objects. It is made to be a replacement for a
 *	call to GetLinkedObjs with the following switches:
 *
 *		exec GetLinkedObjs$
 *			'81',		-- @objId
 *			528482304,	-- @grfcpt
 *			1,			-- @fBaseClasses
 *			1,			-- @fSubClasses
 *			1,			-- @fRecurse
 *			-1,			-- @nRefDirection
 *			null,		-- @riid
 *			0			-- @fCalcOrdKey
 *
 *	fnRefsGetToObj has been shown to run approximately 26 times faster than this
 *	this call to GetLinkedObjs$.
 *
 * Parameters:
 *	None.
 *
 * Returns:
 *	An error on failure or 0 on succes.
 *
 * fnGetRefsToObj Paramters:
 *	A single Object ID (integer)
 *
 * fnGetRefsToObj Returns:
 * 	A table of references to the object
 *
 * Notes:
 *	This only script creates the stored procedure. It must be executed later to
 *	generate fnGetRefsToObj.
 *
 *	This procedure should be called whenever there is a model change. As such it is
 *	called by Class$ and Field$ triggers, and by the database creation routines.
 *
 *	Currently fnGetRefsToObj handeles only one object at a time, primarily because it doesn't
 *	need to handle multiple objects currently. Both calls to it currently are for single
 *	objects.
 *
 * Possible Performance Optimizations for fnGetRefsToObj:
 *	fnGetRefsToObj can possibly optimized in several ways:
 *		1.	Currently parent classes are called for each object. This means, for example,
 *			that the IF block for class 0 will be called once per object. It would probably
 *			be faster to check class 0 once per all owned objects. This would probably be
 *			as easy a change as changing the loop (with the cursor), but I already don't
 *			have time to do everything that needs to be done, and I have to let this go.
 *		2.	It is not entirely clear to me that all the calling programs need all the data
 *			that the function returns. If not, we can stop slinging around so much data,
 *			which will reduce processing time.
 *		3.	A great many incoming references refence ancestor classes only in a minority of
 *			cases. For example, A MoPhonolRuleApp refereces class 0, but only when looking at
 *			a MoPhonolRuleApp object. Currently I have the program checking an incoming
 *			MoPhonolRuleApp even when looking at, say, a Scripture object. I wish I could
 *			figure a way to tell the program to check only certain incoming references for
 *			MoPhonolRuleApp, and ignore the rest. (Hope that made sense.) This has the
 *			potential to cut processing by at least half, and probably a great deal more.
 **********************************************************************************************/

IF OBJECT_ID('CreateGetRefsToObj') IS NOT NULL BEGIN
	PRINT 'removing procedure CreateGetRefsToObj'
	DROP PROCEDURE CreateGetRefsToObj
END
GO
PRINT 'creating procedure CreateGetRefsToObj'
GO

CREATE PROCEDURE CreateGetRefsToObj
AS
	DECLARE
		@fDebug BIT,
		@nFieldId INT,
		@nDstCls INT,
		@nDstCls2 INT,
		@nvcClassName NVARCHAR(100),
		@nvcFieldName NVARCHAR(100),
		@nFieldClass INT,
		@nFieldType INT,
		@nvcProcName NVARCHAR(120),  --( max possible size + a couple spare
		@nvcQ VARCHAR(4000),
		@nvcQuery1 VARCHAR(4000), --( 4000's not big enough; need more than 1 string
		@nvcQuery2 VARCHAR(4000),
		@nvcQuery3 VARCHAR(4000),
		@nvcQuery4 VARCHAR(4000),
		@nvcQuery5 VARCHAR(4000),
		@nvcQuery6 VARCHAR(4000),
		@nvcQuery7 VARCHAR(4000),
		@nvcQuery8 VARCHAR(4000),
		@nvcQuery9 VARCHAR(4000),
		@nvcQuery10 VARCHAR(4000),
		@nvcQuery11 VARCHAR(4000),
		@nvcQuery12 VARCHAR(4000),
		@nvcQuery13 VARCHAR(4000),
		@nvcQuery14 VARCHAR(4000),
		@nvcQuery15 VARCHAR(4000),
		@nFetchStatus BIT,
		@fFirstIf BIT,
		@fFirstIfQuery BIT,
		@nError INT

	SET @fDebug = 0 --( 0 produces stored procedure, 1 produces print
	SET @nvcQ = ''
	SET @nvcQuery1 = ''
	SET @nvcQuery2 = ''
	SET @nvcQuery3 = ''
	SET @nvcQuery4 = ''
	SET @nvcQuery5 = ''
	SET @nvcQuery6 = ''
	SET @nvcQuery7 = ''
	SET @nvcQuery8 = ''
	SET @nvcQuery9 = ''
	SET @nvcQuery10 = ''
	SET @nvcQuery11 = ''
	SET @nvcQuery12 = ''
	SET @nvcQuery13 = ''
	SET @nvcQuery14 = ''
	SET @nvcQuery15 = ''
	set @nError = 0

	--( Loop for subclasses

	IF OBJECT_ID('fnGetRefsToObj') IS NULL
		SET @nvcQuery1 = N'CREATE FUNCTION fnGetRefsToObj (' + CHAR(13)
	ELSE
		SET @nvcQuery1 = N'ALTER FUNCTION fnGetRefsToObj (' + CHAR(13)

	SET @nvcQuery1 = @nvcQuery1 +
		CHAR(9) + N'@nObjId INT, ' + CHAR(13) +
		CHAR(9) + N'@nClassId INT = NULL) ' + CHAR(13) +
		N'RETURNS @tblR TABLE ( ' + CHAR(13) +
		CHAR(9) + N'ObjId INT, ' + CHAR(13) +
		CHAR(9) + N'ObjClass INT, ' + CHAR(13) +
		CHAR(9) + N'ObjLevel INT, ' + CHAR(13) +
		CHAR(9) + N'RefObjId INT, ' + CHAR(13) +
		CHAR(9) + N'RefObjClass INT, ' + CHAR(13) +
		CHAR(9) + N'RefObjField INT, ' + CHAR(13) +
		CHAR(9) + N'RefObjFieldOrder INT, ' + CHAR(13) +
		CHAR(9) + N'RefObjFieldType INT) ' + CHAR(13) +
		N'AS BEGIN ' + CHAR(13) +
		CHAR(13) +
		N'/* == This function generated by CreateGetRefsToObj == */ ' + CHAR(13) +
		CHAR(13) +
		N'DECLARE @nDst INT, @nFetchStatus INT, @nObjLevel INT;' + CHAR(13) +
		N'DECLARE @tblO TABLE (Id INT, ObjLevel INT, Class INT)' + CHAR(13) +
		N'IF @nClassId IS NULL ' + CHAR(13) +
		CHAR(9) + 'SELECT @nClassId = Class$ FROM CmObject WHERE [ID] = @nObjId' + CHAR(13) +
		CHAR(13) +
		N'/* Get Owned objects */ ' + CHAR(13) +
		N'SET @nObjLevel = 1;'  + CHAR(13) +
		N'INSERT INTO @tblO (ID, ObjLevel, Class)' + CHAR(13) +
		CHAR(9) + N'VALUES (@nObjId, @nObjLevel, @nClassId)' + CHAR(13) +
		N'WHILE @@ROWCOUNT != 0 BEGIN' + CHAR(13) +
		CHAR(9) + N'SET @nObjLevel = @nObjLevel + 1;' + CHAR(13) +
		CHAR(9) + N'INSERT INTO @tblO' + CHAR(13) +
		CHAR(9) + CHAR(9) + N'SELECT co.Id, @nObjLevel, co.Class$' +CHAR(13) +
		CHAR(9) + CHAR(9) + N'FROM @tblO t ' + CHAR(13) +
		CHAR(9) + CHAR(9) + N'JOIN CmObject co ON co.Owner$ = t.Id' + CHAR(13) +
		CHAR(9) + CHAR(9) + N'WHERE t.ObjLevel = @nObjLevel - 1;' + CHAR(13) +
		N'END;' + CHAR(13) +
		CHAR(13) +
		N'/* Get super classes of the objects */ ' + CHAR(13) +
		N'INSERT INTO @tblO' + CHAR(13) +
		N'SELECT o.Id, o.ObjLevel, cp.dst' + CHAR(13) +
		N'FROM @tblO o' + CHAR(13) +
		N'JOIN ClassPar$ cp ON cp.Src = o.Class' + CHAR(13) +
		N'WHERE cp.Depth != 0' + CHAR(13) +
		CHAR(13) +
		N'/* Now get references to them */ ' + CHAR(13) +
		N'DECLARE curClassDepth CURSOR ' + CHAR(13) +
		CHAR(9) + N'FOR SELECT Id, Class, ObjLevel - 1 FROM @tblO '  + CHAR(13) +
		N'OPEN curClassDepth;' + CHAR(13) +
		N'FETCH NEXT FROM curClassDepth INTO @nObjId, @nDst, @nObjLevel;' + CHAR(13) +
		N'SET @nFetchStatus = @@FETCH_STATUS;' + CHAR(13) +
		N'WHILE @nFetchStatus = 0 BEGIN ' + CHAR(13) +
		CHAR(9) + N'--( Some of these unions might need to be broken apart if there are too many of them.' + CHAR(13)

	DECLARE curRefs CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
		SELECT f.Id, f.Class, f.DstCls, c.Name AS ClassName, f.Name AS FieldName, f.Type
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.Type IN (24, 26, 28)
		ORDER BY f.DstCls, f.Id

	SET @nDstCls2 = 987654321	--( bogus ID
	SET @fFirstIf = 1

	OPEN curRefs
	FETCH curRefs INTO @nFieldId, @nFieldClass, @nDstCls, @nvcClassName, @nvcFieldName, @nFieldType
	SET @nFetchStatus = @@FETCH_STATUS
	WHILE @nFetchStatus = 0 BEGIN
		--( Create an IF block for a particular class

		IF @nDstCls != @nDstCls2 BEGIN
			SET @nvcQ = CHAR(9)
			IF @fFirstIf = 0
				SET @nvcQ = @nvcQ + N'ELSE '
			SET @nvcQ = @nvcQ + N'IF @nDst = ' + CONVERT(NVARCHAR(10), @nDstCls) + N' BEGIN ' + CHAR(13)
			SET @nDstCls2 = @nDstCls
		END

		IF LEN(@nvcQuery1) < 3750
			SET @nvcQuery1 = @nvcQuery1 + @nvcQ
		ELSE IF LEN(@nvcQuery2) < 3750
			SET @nvcQuery2 = @nvcQuery2 + @nvcQ
		ELSE IF LEN(@nvcQuery3) < 3750
			SET @nvcQuery3 = @nvcQuery3 + @nvcQ
		ELSE IF LEN(@nvcQuery4) < 3750
			SET @nvcQuery4 = @nvcQuery4 + @nvcQ
		ELSE IF LEN(@nvcQuery5) < 3750
			SET @nvcQuery5 = @nvcQuery5 + @nvcQ
		ELSE IF LEN(@nvcQuery6) < 3750
			SET @nvcQuery6 = @nvcQuery6 + @nvcQ
		ELSE IF LEN(@nvcQuery7) < 3750
			SET @nvcQuery7 = @nvcQuery7 + @nvcQ
		ELSE IF LEN(@nvcQuery8) < 3750
			SET @nvcQuery8 = @nvcQuery8 + @nvcQ
		ELSE IF LEN(@nvcQuery9) < 3750
			SET @nvcQuery9 = @nvcQuery9 + @nvcQ
		ELSE IF LEN(@nvcQuery10) < 3750
			SET @nvcQuery10 = @nvcQuery10 + @nvcQ
		ELSE IF LEN(@nvcQuery11) < 3750
			SET @nvcQuery11 = @nvcQuery11 + @nvcQ
		ELSE IF LEN(@nvcQuery12) < 3750
			SET @nvcQuery12 = @nvcQuery12 + @nvcQ
		ELSE IF LEN(@nvcQuery13) < 3750
			SET @nvcQuery13 = @nvcQuery13 + @nvcQ
		ELSE IF LEN(@nvcQuery14) < 3750
			SET @nvcQuery14 = @nvcQuery14 + @nvcQ
		ELSE IF LEN(@nvcQuery15) < 3750
			SET @nvcQuery15 = @nvcQuery15 + @nvcQ

		--( Cycle through the classes that refer to this one.

		SET @fFirstIfQuery = 1
		WHILE @nDstCls2 = @nDstCls AND @nFetchStatus = 0 BEGIN
			IF @fFirstIfQuery = 1
				SET @nvcQ = CHAR(9) + CHAR(9) + N'INSERT INTO @tblR ' + CHAR(13)
			ELSE
				SET @nvcQ = CHAR(13) + CHAR(9) + CHAR(9) + N'UNION ' + CHAR(13)
			SET @fFirstIfQuery = 0


			IF @nFieldType = 24
				SET @nvcQ = @nvcQ +
					CHAR(9) + CHAR(9) + CHAR(9) + N'SELECT @nObjId, @nDst, @nObjLevel, r.[Id], '
						+ CONVERT(NVARCHAR(10), @nFieldClass) + N', ' +
						+ CONVERT(NVARCHAR(10), @nFieldId) +
						+ N', NULL, '
						+ CONVERT(NVARCHAR(10), @nFieldType) + CHAR(13) +
					CHAR(9) + CHAR(9) + CHAR(9) + N'FROM [' + @nvcClassName + N'] r ' + CHAR(13) +
					CHAR(9) + CHAR(9) + CHAR(9) + N'LEFT OUTER JOIN @tblO o ON o.[Id] = r.[Id] ' + CHAR(13) +
					CHAR(9) + CHAR(9) + CHAR(9) + N'WHERE r.[' + @nvcFieldName + N'] = @nObjId ' +
					N'AND o.[Id] IS NULL'
			ELSE BEGIN
				SET @nvcQ = @nvcQ +
					CHAR(9) + CHAR(9) + CHAR(9) + N'SELECT @nObjId, @nDst, @nObjLevel, r.Src, ' +
					+ CONVERT(NVARCHAR(10), @nFieldClass) + N', ' +
					+ CONVERT(NVARCHAR(10), @nFieldId)

				IF @nFieldType = 26
					SET @nvcQ = @nvcQ + N', NULL, '
				ELSE
					SET @nvcQ = @nvcQ + N', r.Ord, '

				SET @nvcQ = @nvcQ +
					CONVERT(NVARCHAR(10), @nFieldType) + CHAR(13) +
					CHAR(9) + CHAR(9) + CHAR(9) + N'FROM ' + @nvcClassName + N'_' + @nvcFieldName + N' r ' + CHAR(13) +
					CHAR(9) + CHAR(9) + CHAR(9) + N'LEFT OUTER JOIN @tblO o ON o.Id = r.Src ' + CHAR(13) +
					CHAR(9) + CHAR(9) + CHAR(9) + N'WHERE r.Dst = @nObjId AND o.Id IS NULL'
			END
			IF LEN(@nvcQuery1) < 3750
				SET @nvcQuery1 = @nvcQuery1 + @nvcQ
			ELSE IF LEN(@nvcQuery2) < 3750
				SET @nvcQuery2 = @nvcQuery2 + @nvcQ
			ELSE IF LEN(@nvcQuery3) < 3750
				SET @nvcQuery3 = @nvcQuery3 + @nvcQ
			ELSE IF LEN(@nvcQuery4) < 3750
				SET @nvcQuery4 = @nvcQuery4 + @nvcQ
			ELSE IF LEN(@nvcQuery5) < 3750
				SET @nvcQuery5 = @nvcQuery5 + @nvcQ
			ELSE IF LEN(@nvcQuery6) < 3750
				SET @nvcQuery6 = @nvcQuery6 + @nvcQ
			ELSE IF LEN(@nvcQuery7) < 3750
				SET @nvcQuery7 = @nvcQuery7 + @nvcQ
			ELSE IF LEN(@nvcQuery8) < 3750
				SET @nvcQuery8 = @nvcQuery8 + @nvcQ
			ELSE IF LEN(@nvcQuery9) < 3750
				SET @nvcQuery9 = @nvcQuery9 + @nvcQ
			ELSE IF LEN(@nvcQuery10) < 3750
				SET @nvcQuery10 = @nvcQuery10 + @nvcQ
			ELSE IF LEN(@nvcQuery11) < 3750
				SET @nvcQuery11 =@nvcQuery11 + @nvcQ
			ELSE IF LEN(@nvcQuery12) < 3750
				SET @nvcQuery12 = @nvcQuery12 + @nvcQ
			ELSE IF LEN(@nvcQuery13) < 3750
				SET @nvcQuery13 = @nvcQuery13 + @nvcQ
			ELSE IF LEN(@nvcQuery14) < 3750
				SET @nvcQuery14 = @nvcQuery14 + @nvcQ
			ELSE IF LEN(@nvcQuery15) < 3750
				SET @nvcQuery15 = @nvcQuery15 + @nvcQ

			FETCH curRefs INTO @nFieldId, @nFieldClass, @nDstCls, @nvcClassName, @nvcFieldName, @nFieldType
			SET @nFetchStatus = @@FETCH_STATUS
		END

		--( Close out the if block
		IF LEN(@nvcQuery1) < 3750
			SET @nvcQuery1 = @nvcQuery1 + ';' + CHAR(13) + CHAR(9) + N'END;' + CHAR(13)
		ELSE IF LEN(@nvcQuery2) < 3750
			SET @nvcQuery2 = @nvcQuery2 + ';' + CHAR(13) + CHAR(9) + N'END;' + CHAR(13)
		ELSE IF LEN(@nvcQuery3) < 3750
			SET @nvcQuery3 = @nvcQuery3 + ';' + CHAR(13) + CHAR(9) + N'END;' + CHAR(13)
		ELSE IF LEN(@nvcQuery4) < 3750
			SET @nvcQuery4 = @nvcQuery4 + ';' + CHAR(13) + CHAR(9) + N'END;' + CHAR(13)
		ELSE IF LEN(@nvcQuery5) < 3750
			SET @nvcQuery5 = @nvcQuery5 + ';' + CHAR(13) + CHAR(9) + N'END;' + CHAR(13)
		ELSE IF LEN(@nvcQuery6) < 3750
			SET @nvcQuery6 = @nvcQuery6 + ';' + CHAR(13) + CHAR(9) + N'END;' + CHAR(13)
		ELSE IF LEN(@nvcQuery7) < 3750
			SET @nvcQuery7 = @nvcQuery7 + ';' + CHAR(13) + CHAR(9) + N'END;' + CHAR(13)
		ELSE IF LEN(@nvcQuery8) < 3750
			SET @nvcQuery8 = @nvcQuery8 + ';' + CHAR(13) + CHAR(9) + N'END;' + CHAR(13)
		ELSE IF LEN(@nvcQuery9) < 3750
			SET @nvcQuery9 = @nvcQuery9 + ';' + CHAR(13) + CHAR(9) + N'END;' + CHAR(13)
		ELSE IF LEN(@nvcQuery10) < 3750
			SET @nvcQuery10 = @nvcQuery10 + ';' + CHAR(13) + CHAR(9) + N'END;' + CHAR(13)
		ELSE IF LEN(@nvcQuery11) < 3750
			SET @nvcQuery11 = @nvcQuery11 + ';' + CHAR(13) + CHAR(9) + N'END;' + CHAR(13)
		ELSE IF LEN(@nvcQuery12) < 3750
			SET @nvcQuery12 = @nvcQuery12 + ';' + CHAR(13) + CHAR(9) + N'END;' + CHAR(13)
		ELSE IF LEN(@nvcQuery13) < 3750
			SET @nvcQuery13 = @nvcQuery13 + ';' + CHAR(13) + CHAR(9) + N'END;' + CHAR(13)
		ELSE IF LEN(@nvcQuery14) < 3750
			SET @nvcQuery14 = @nvcQuery14 + ';' + CHAR(13) + CHAR(9) + N'END;' + CHAR(13)
		ELSE IF LEN(@nvcQuery15) < 3750
			SET @nvcQuery15 = @nvcQuery15 + ';' + CHAR(13) + CHAR(9) + N'END;' + CHAR(13)

		SET @fFirstIf = 0
	END --( @@FETCH_STATUS = 0
	CLOSE curRefs
	DEALLOCATE curRefs

	SET @nvcQ =
		CHAR(9) + N'FETCH NEXT FROM curClassDepth INTO @nObjId, @nDst, @nObjLevel; ' + CHAR(13) +
		CHAR(9) + N'SET @nFetchStatus = @@FETCH_STATUS; ' + CHAR(13) +
		N'END; ' + CHAR(13) +
		N'RETURN; ' + CHAR(13) +
		N'END ' + CHAR(13)

	IF LEN(@nvcQuery1) < 3750
		SET @nvcQuery1 = @nvcQuery1 + @nvcQ
	ELSE IF LEN(@nvcQuery2) < 3750
		SET @nvcQuery2 = @nvcQuery2 + @nvcQ
	ELSE IF LEN(@nvcQuery3) < 3750
		SET @nvcQuery3 = @nvcQuery3 + @nvcQ
	ELSE IF LEN(@nvcQuery4) < 3750
		SET @nvcQuery4 = @nvcQuery4 + @nvcQ
	ELSE IF LEN(@nvcQuery5) < 3750
		SET @nvcQuery5 = @nvcQuery5 + @nvcQ
	ELSE IF LEN(@nvcQuery6) < 3750
		SET @nvcQuery6 = @nvcQuery6 + @nvcQ
	ELSE IF LEN(@nvcQuery7) < 3750
		SET @nvcQuery7 = @nvcQuery7 + @nvcQ
	ELSE IF LEN(@nvcQuery8) < 3750
		SET @nvcQuery8 = @nvcQuery8 + @nvcQ
	ELSE IF LEN(@nvcQuery9) < 3750
		SET @nvcQuery9 = @nvcQuery9 + @nvcQ
	ELSE IF LEN(@nvcQuery10) < 3750
		SET @nvcQuery10 = @nvcQuery10 + @nvcQ
	ELSE IF LEN(@nvcQuery11) < 3750
		SET @nvcQuery11 = @nvcQuery11 + @nvcQ
	ELSE IF LEN(@nvcQuery12) < 3750
		SET @nvcQuery12 = @nvcQuery12 + @nvcQ
	ELSE IF LEN(@nvcQuery13) < 3750
		SET @nvcQuery13 = @nvcQuery13 + @nvcQ
	ELSE IF LEN(@nvcQuery14) < 3750
		SET @nvcQuery14 = @nvcQuery14 + @nvcQ
	ELSE IF LEN(@nvcQuery15) < 3750
		SET @nvcQuery15 = @nvcQuery15 + @nvcQ

	IF @fDebug = 0 BEGIN
		EXEC (
			@nvcQuery1 + N' ' + @nvcQuery2 + N' ' + @nvcQuery3 + N' ' +
			@nvcQuery4 + N' ' + @nvcQuery5 + N' ' + @nvcQuery6 + N' ' +
			@nvcQuery7 + N' ' + @nvcQuery8 + N' ' + @nvcQuery9 + N' ' +
			@nvcQuery10 + N' ' + @nvcQuery11 + N' ' + @nvcQuery12 + N' ' +
			@nvcQuery13 + N' ' + @nvcQuery14 + N' ' + @nvcQuery15)
		SET @nError = @@ERROR
	END
	ELSE BEGIN
		PRINT @nvcQuery1
		PRINT '--(Starting @nvcQuery2'
		PRINT @nvcQuery2
		PRINT '--(Starting @nvcQuery3'
		PRINT @nvcQuery3
		PRINT '--(Starting @nvcQuery4'
		PRINT @nvcQuery4
		PRINT '--(Starting @nvcQuery5'
		PRINT @nvcQuery5
		PRINT '--(Starting @nvcQuery6'
		PRINT @nvcQuery6
		PRINT '--(Starting @nvcQuery7'
		PRINT @nvcQuery7
		PRINT '--(Starting @nvcQuery8'
		PRINT @nvcQuery8
		PRINT '--(Starting @nvcQuery9'
		PRINT @nvcQuery9
		PRINT '--(Starting @nvcQuery10'
		PRINT @nvcQuery10
		PRINT '--(Starting @nvcQuery11'
		PRINT @nvcQuery11
		PRINT '--(Starting @nvcQuery12'
		PRINT @nvcQuery12
		PRINT '--(Starting @nvcQuery13'
		PRINT @nvcQuery13
		PRINT '--(Starting @nvcQuery14'
		PRINT @nvcQuery14
		PRINT '--(Starting @nvcQuery15'
		PRINT @nvcQuery15
	END

	RETURN @nError
GO