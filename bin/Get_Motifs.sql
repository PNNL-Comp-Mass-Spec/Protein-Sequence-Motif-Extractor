-- Extract data from
-- MT_Human_Glycated_Plasma_P587
--  and
-- MT_Human_Glycated_RBC_P588

-- If necessary, clear all entries in T_Protein_SeqsWithMods
-- TRUNCATE TABLE T_Protein_SeqsWithMods

-- Populate T_Protein_SeqsWithMods

-- Change Hexose to #    (tyipcally K#)
-- Change Plus1Oxy to *  (typically M*)
-- Ignore IodoAcet since typically a static mod on C

exec UpdateProteinSeqsWithModsTable @RefIDMin=0, @RefIDMax=0, @MinimumPMTQualityScore=1, @ModNamesAndSymbols = 'Hexose=#, Plus1Oxy=*'


-- Obtain the Results (marked up protein sequence for each protein)
-- Note: You must use Tasks->Export data to obtain these results from SSMS since Protein_Sequence_with_Mods can be over 8000 characters long (and SSMS truncates at 8000 characters)
SELECT P.Reference,
       P.Description,
       PSM.Protein_Sequence_with_Mods,
       PSM.Protein_Residue_Count,
       PSM.Mod_Symbol_Count,
       PSM.Ref_ID
FROM T_Protein_SeqsWithMods PSM
     INNER JOIN T_Proteins P
       ON PSM.Ref_ID = P.Ref_ID
WHERE (PSM.Mod_Symbol_Count > 0) AND
      (PSM.Protein_Sequence_with_Mods LIKE '%#%')


-- Get all peptides with K#
-- This does not include any protein context info
SELECT MT.Mass_Tag_ID,
       MT.Monoisotopic_Mass,
       MT.Mod_Count,
       MT.Mod_Description,
       MT.PeptideEx,
       MT.Peptide
FROM T_Mass_Tags MT
     INNER JOIN T_Mass_Tag_Mod_Info MTMI
       ON MT.Mass_Tag_ID = MTMI.Mass_Tag_ID
WHERE (MTMI.Mod_Name = 'hexose') AND
      PMT_Quality_Score >= 1
GROUP BY MT.Mass_Tag_ID, MT.Peptide, MT.Monoisotopic_Mass, 
         MT.Mod_Count, MT.Mod_Description, MT.PeptideEx
ORDER BY MT.Mass_Tag_ID


-- Get the peptide to protein mapping
SELECT MT.Mass_Tag_ID,
       MTPM.Ref_ID,
       Prot.Reference,
       MTPM.Residue_Start,
       MTPM.Residue_End,
       MTPM.Cleavage_State,
       MTPM.Terminus_State
FROM T_Mass_Tags MT
     INNER JOIN T_Mass_Tag_Mod_Info MTMI
       ON MT.Mass_Tag_ID = MTMI.Mass_Tag_ID
     INNER JOIN T_Mass_Tag_to_Protein_Map MTPM
       ON MT.Mass_Tag_ID = MTPM.Mass_Tag_ID
     INNER JOIN T_Proteins Prot
       ON MTPM.Ref_ID = Prot.Ref_ID
WHERE (MTMI.Mod_Name = 'hexose') AND
      (MT.PMT_Quality_Score >= 1)
GROUP BY MT.Mass_Tag_ID, MTPM.Ref_ID, Prot.Reference, 
         MTPM.Residue_Start, MTPM.Residue_End, 
         MTPM.Cleavage_State, MTPM.Terminus_State
ORDER BY MT.Mass_Tag_ID, MTPM.Ref_ID
