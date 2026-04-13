using BinarySerializer.Ray1;
using BinarySerializer.Ray1.Jaguar;

namespace RayCarrot.RCP.Metro;

public static class Rayman1Progression
{
    public static IReadOnlyList<GameProgressionDataItem> CreateProgressionItems(
        SaveSlot saveSlot, 
        out int collectiblesCount, 
        out int maxCollectiblesCount)
    {
        // Get total amount of cages
        int cages = saveSlot.WorldInfoSaveZone.Sum(x => x.Cages);

        GameProgressionDataItem[] dataItems =
        {
            new GameProgressionDataItem(
                isPrimaryItem: true,
                icon: ProgressionIconAsset.R1_Cage,
                header: new ResourceLocString(nameof(Resources.Progression_Cages)),
                value: cages,
                max: 102),
            new GameProgressionDataItem(
                isPrimaryItem: false,
                icon: ProgressionIconAsset.R1_Continue,
                header: new ResourceLocString(nameof(Resources.Progression_Continues)),
                value: saveSlot.ContinuesCount),
            new GameProgressionDataItem(
                isPrimaryItem: false,
                icon: ProgressionIconAsset.R1_Life,
                header: new ResourceLocString(nameof(Resources.Progression_Lives)),
                value: saveSlot.StatusBar.LivesCount),
        };

        collectiblesCount = cages;
        maxCollectiblesCount = 102;

        return dataItems;
    }

    public static IReadOnlyList<GameProgressionDataItem> CreateProgressionItems(
        JAG_SaveSlot saveSlot, 
        out int collectiblesCount, 
        out int maxCollectiblesCount)
    {
        int cages = 0;

        void addCages(JAG_SaveLevel saveLevel)
        {
            if (saveLevel.Cage1)
                cages++;
            if (saveLevel.Cage2)
                cages++;
            if (saveLevel.Cage3)
                cages++;
            if (saveLevel.Cage4)
                cages++;
            if (saveLevel.Cage5)
                cages++;
            if (saveLevel.Cage6)
                cages++;
        }

        // Calculate cages from each level
        addCages(saveSlot.PinkPlantWoods);
        addCages(saveSlot.AnguishLagoon);
        addCages(saveSlot.ForgottenSwamps);
        addCages(saveSlot.MoskitosNest);
        addCages(saveSlot.BongoHills);
        addCages(saveSlot.AllegroPresto);
        addCages(saveSlot.GongHeights);
        addCages(saveSlot.MrSaxsHullaballoo);
        addCages(saveSlot.TwilightGulch);
        addCages(saveSlot.TheHardRocks);
        addCages(saveSlot.MrStonesPeaks);
        addCages(saveSlot.EraserPlains);
        addCages(saveSlot.PencilPentathlon);
        addCages(saveSlot.SpaceMamasCrater);
        addCages(saveSlot.CrystalPalace);
        addCages(saveSlot.EatatJoes);
        addCages(saveSlot.MrSkopsStalactites);

        // Convert from BCD
        int livesCount = 0;
        livesCount += 10 * (saveSlot.LivesCount >> 4);
        livesCount += saveSlot.LivesCount & 0xF;

        GameProgressionDataItem[] dataItems =
        [
            new GameProgressionDataItem(
                isPrimaryItem: true,
                icon: ProgressionIconAsset.R1_Cage,
                header: new ResourceLocString(nameof(Resources.Progression_Cages)),
                value: cages,
                max: 102),
            new GameProgressionDataItem(
                isPrimaryItem: false,
                icon: ProgressionIconAsset.R1_Continue,
                header: new ResourceLocString(nameof(Resources.Progression_Continues)),
                value: saveSlot.ContinuesCount),
            new GameProgressionDataItem(
                isPrimaryItem: false,
                icon: ProgressionIconAsset.R1_Life,
                header: new ResourceLocString(nameof(Resources.Progression_Lives)),
                value: livesCount)
        ];

        collectiblesCount = cages;
        maxCollectiblesCount = 102;

        return dataItems;
    }
}