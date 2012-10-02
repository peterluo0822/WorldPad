-- update database FROM version 200144 to 200145
BEGIN TRANSACTION  --( will be rolled back if wrong version#)
if object_id('RemoveUnusedAnalyses$') is not null begin
	print 'removing proc RemoveUnusedAnalyses$'
	drop proc [RemoveUnusedAnalyses$]
end
go
print 'creating proc RemoveUnusedAnalyses$'
go

/*****************************************************************************
 *	Procedure: RemoveUnusedAnalyses$
 *
 *	Description:
 *		Performs 2 tasks:
 *		1) Deletes any "stale" evaluations, those that have an old date that
 *			are no longer valid.
 *		2) Removes Word form Analyses that are not referenced by an
 *			evaluation.
 *
 *	Parameters:
 * 		@nAgentID			ID of the agent
 *		@nWfiWordFormID		ID of the wordform
 *		@dtEval			Date-time of the evaluation
 *
 *  Selects:
 *		0 if everything has been deleted (or an error has occurred), 1 if there
 *		is more stuff remaining to delete (deletes a maximum of 16 objects at a
 *		 time).
 *	Returns:
 *		0 for success, otherwise the error code returned by DeleteObj$
 *****************************************************************************/

-- TODO (SteveMiller/RandyR): Determine if the orphaned records should really
--							be deleted by a trigger.

CREATE PROCEDURE RemoveUnusedAnalyses$
	@nAgentId INT,
	@nWfiWordFormID INT,
	@dtEval DATETIME
AS
	DECLARE
		@nIsNoCountOn INT,
		@nGonnerID INT,
		@nError INT,
		@fMoreToDelete INT

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON

	SET @nGonnerId = NULL
	SET @nError = 0
	SET @fMoreToDelete = 0

	--== Delete all evaluations with null targets. ==--
	SELECT TOP 1 @nGonnerId = ae.[Id]
	FROM CmAgentEvaluation ae (READUNCOMMITTED)
	JOIN CmObject objae (READUNCOMMITTED)
		ON objae.[Id] = ae.[Id]
	WHERE ae.Target IS NULL
	ORDER BY ae.[Id]

	IF @@ROWCOUNT != 0 BEGIN
		EXEC @nError = DeleteObj$ @nGonnerId
		SET @fMoreToDelete = 1
		GOTO Finish
	END

	--== Delete stale evaluations on analyses ==--
	SELECT TOP 1 @nGonnerId = ae.[Id]
	FROM CmAgentEvaluation ae (READUNCOMMITTED)
	JOIN CmObject objae (READUNCOMMITTED)
		ON objae.[Id] = ae.[Id] AND objae.Owner$ = @nAgentId
	JOIN CmObject objanalysis (READUNCOMMITTED)
		ON objanalysis.[Id] = ae.Target
		AND objanalysis.Class$ = 5059 -- WfiAnalysis objects
		AND objanalysis.Owner$ = @nWfiWordFormID
	WHERE ae.DateCreated < @dtEval
	ORDER BY ae.[Id]

	IF @@ROWCOUNT != 0 BEGIN
		EXEC @nError = DeleteObj$ @nGonnerId
		SET @fMoreToDelete = 1
		GOTO Finish
	END

	--== Make sure all analyses have human evaluations, if they, or glosses they own, are referred to by a WIC annotation. ==--
	DECLARE @adID INT, @analId INT, @humanAgentId INT, @rowcount INT, @rowcount2 INT, @evalId INT

	-- Get the ID of the CmAnnotationDefn that is the WIC type.
	SELECT @adID=Id
	FROM CmObject (READUNCOMMITTED)
	WHERE Guid$='eb92e50f-ba96-4d1d-b632-057b5c274132'

	-- Get Id of the first 'default user' human agent
	SELECT TOP 1 @humanAgentId = a.Id
	FROM CmAgent a (READUNCOMMITTED)
	JOIN CmAgent_Name nme (READUNCOMMITTED)
		ON a.Id = nme.Obj
	WHERE a.Human = 1 AND nme.Txt = 'default user'

	SELECT TOP 1 @analId = wa.[Id]
	FROM WfiAnalysis_ wa (READUNCOMMITTED)
	left outer JOIN WfiGloss_ gloss (READUNCOMMITTED)
		ON gloss.Owner$ = wa.Id
	JOIN CmAnnotation ann (READUNCOMMITTED)
		ON ann.InstanceOf = wa.[Id] OR ann.[InstanceOf] = gloss.[Id]
	JOIN CmObject ad (readuncommitted)
		ON ann.AnnotationType = ad.Id AND ad.Id = @adID
	WHERE wa.[Owner$] = @nWfiWordFormID
	ORDER BY wa.[Id]

	WHILE @@ROWCOUNT != 0 BEGIN
		SELECT @evalId=Id
		FROM cmAgentEvaluation_ cae (READUNCOMMITTED)
		WHERE Target = @analId AND Owner$ = @humanAgentId

		IF @@ROWCOUNT = 0
		BEGIN
			EXEC @nError = SetAgentEval
				@humanAgentId,
				@analId,
				1,
				'Set by RemoveUnusedAnalyses$',
				@dtEval
			SET @fMoreToDelete = 1
			GOTO Finish
		END

		SELECT TOP 1 @analId = wa.[Id]
		FROM WfiAnalysis_ wa (READUNCOMMITTED)
		left outer JOIN WfiGloss_ gloss (READUNCOMMITTED)
			ON gloss.Owner$ = wa.Id
		JOIN CmAnnotation ann (READUNCOMMITTED)
			ON ann.InstanceOf = wa.[Id] OR ann.[InstanceOf] = gloss.[Id]
		JOIN CmObject ad (readuncommitted)
			ON ann.AnnotationType = ad.Id AND ad.Id = @adID
		WHERE wa.[Id] > @analId AND wa.[Owner$] = @nWfiWordFormID
		ORDER BY wa.[Id]
	END

	--== Delete orphan analyses, which have no evaluations ==--
	SELECT TOP 1 @nGonnerId = analysis.[Id]
	FROM CmObject analysis (READUNCOMMITTED)
	LEFT OUTER JOIN cmAgentEvaluation cae (READUNCOMMITTED)
		ON cae.Target = analysis.[Id]
	WHERE cae.Target IS NULL
		AND analysis.OwnFlid$ = 5062002		-- kflidWfiWordform_Analyses
		AND analysis.Owner$ = @nWfiWordFormID
	ORDER BY analysis.[Id]

	WHILE @@ROWCOUNT != 0 BEGIN
		EXEC @nError = DeleteObj$ @nGonnerId
		SET @fMoreToDelete = 1
		GOTO Finish
	END

Finish:
	IF @nIsNocountOn = 0 SET NOCOUNT OFF
	SELECT @fMoreToDelete AS MoreToDelete
	RETURN @nError

GO
-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200145
BEGIN
	UPDATE [Version$] SET [DbVer] = 200146
	COMMIT TRANSACTION
	PRINT 'database updated to version 200146'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200145 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO