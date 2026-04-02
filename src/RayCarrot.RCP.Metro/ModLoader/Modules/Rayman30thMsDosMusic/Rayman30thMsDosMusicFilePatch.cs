using System.IO;
using BinarySerializer;
using NAudio.Wave;

namespace RayCarrot.RCP.Metro.ModLoader.Modules.Rayman30thMsDosMusic;

public class Rayman30thMsDosMusicFilePatch : IFilePatch
{
    public Rayman30thMsDosMusicFilePatch(ModFilePath path, Rayman30thMsDosMusicModuleGame game, IReadOnlyCollection<Rayman30thMsDosMusicModuleTrack> tracks)
    {
        Path = path;
        Game = game;
        Tracks = tracks;
    }

    // Offsets within the game's CD audio tracks struct
    private const int LastTrackOffset = 2;
    private const int TrackPositionsOffset = LastTrackOffset + 5;
    private const int TotalTrackLengthsOffset = TrackPositionsOffset + MaxTracksCount * 4;
    private const int TrackLengthsOffset = TotalTrackLengthsOffset + 4;
    private const int MaxTracksCount = 100;

    public ModFilePath Path { get; }
    public Rayman30thMsDosMusicModuleGame Game { get; }
    public IReadOnlyCollection<Rayman30thMsDosMusicModuleTrack> Tracks { get; }

    private MSF[] GetExistingTrackLengths(Reader reader)
    {
        // Read the last track
        reader.BaseStream.Position = Game.BootOffset + LastTrackOffset;
        byte lastTrack = reader.ReadByte();

        // Read the offsets and determine the lengths from that (easier than using the LBA table)
        reader.BaseStream.Position = Game.BootOffset + TrackPositionsOffset;
        MSF[] trackLengths = new MSF[MaxTracksCount];
        MSF currentOffset = new(reader.ReadUInt32());
        for (int i = 1; i < lastTrack + 1; i++)
        {
            MSF trackOffset = new(reader.ReadUInt32());
            trackLengths[i - 1] = trackOffset - currentOffset;
            currentOffset = trackOffset;
        }

        // For the last track we need to read the total length and subtract the current offset from it
        reader.BaseStream.Position = Game.BootOffset + TotalTrackLengthsOffset;
        MSF totalLength = new(reader.ReadUInt32());
        totalLength += MSF.Pregap + MSF.Pregap;
        trackLengths[lastTrack] = totalLength - currentOffset;

        return trackLengths;
    }

    public void PatchFile(Stream stream)
    {
        // Create a reader and writer
        using Reader reader = new(stream, leaveOpen: true);
        using Writer writer = new(stream, leaveOpen: true);

        // Get the existing track lengths since we might not be replacing every track
        MSF[] trackLengths = GetExistingTrackLengths(reader);

        // Read the last track
        reader.BaseStream.Position = Game.BootOffset + LastTrackOffset;
        byte lastTrack = reader.ReadByte();

        // Get track lengths from the new audio files
        foreach (Rayman30thMsDosMusicModuleTrack track in Tracks)
        {
            using AudioFileReader audioReader = new(track.FilePath);
            double totalSeconds = audioReader.TotalTime.TotalSeconds;
            trackLengths[track.Track] = MSF.FromSeconds(totalSeconds) + MSF.Pregap;

            // Increase last track if needed
            if (track.Track > lastTrack)
                lastTrack = (byte)track.Track;
        }

        // Write new last track value
        writer.BaseStream.Position = Game.BootOffset + LastTrackOffset;
        writer.Write(lastTrack);

        // Write track offsets
        writer.BaseStream.Position = Game.BootOffset + TrackPositionsOffset;
        MSF currentOffset = new(0, 0, 0);
        for (int i = 0; i < MaxTracksCount; i++)
        {
            if (i > lastTrack)
            {
                writer.Write((uint)0);
            }
            else
            {
                writer.Write(currentOffset.Value);
                currentOffset += trackLengths[i];
            }
        }

        // Remove 2 pregaps (for some reason) to get the total length
        MSF totalLength = currentOffset - MSF.Pregap - MSF.Pregap;

        // Write total length of all tracks in MSF
        writer.BaseStream.Position = Game.BootOffset + TotalTrackLengthsOffset;
        writer.Write(totalLength.Value);

        // Write LBA length tables (2 duplicated tables for some reason)
        writer.BaseStream.Position = Game.BootOffset + TrackLengthsOffset;
        for (int tableIndex = 0; tableIndex < 2; tableIndex++)
        {
            for (int i = 0; i < MaxTracksCount; i++)
            {
                MSF length = trackLengths[i];
                int lba = length.GetLBA();

                // Remove pre-gap for first and last track for some reason
                if (i == 0 || i == lastTrack)
                    lba -= MSF.Pregap.GetLBA();

                writer.Write(lba);
            }
        }
    }
}