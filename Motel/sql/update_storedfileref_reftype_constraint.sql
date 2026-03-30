ALTER TABLE [dbo].[StoredFileReference]
DROP CONSTRAINT [CK_StoredFileReference_RefType];

ALTER TABLE [dbo].[StoredFileReference]
ADD CONSTRAINT [CK_StoredFileReference_RefType]
CHECK ([RefType] IN ('tenant', 'room', 'property', 'ticket', 'meter', 'invoice', 'contract', 'roomfurniture', 'residence_proof'));
