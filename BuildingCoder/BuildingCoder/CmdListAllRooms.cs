#region Header
//
// CmdListAllRooms.cs - list properties from all rooms
//
// Copyright (C) 2011-2016 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using BoundarySegment = Autodesk.Revit.DB.BoundarySegment;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdListAllRooms : IExternalCommand
  {
    /// <summary>
    /// Draft for method to distinguish 'Not Placed', 
    /// 'Redundant' and 'Not Enclosed' rooms.
    /// </summary>
    void DistinguishRoomsDraft(
      Document doc,
      ref StringBuilder sb,
      ref int numErr,
      ref int numWarn )
    {
      FilteredElementCollector rooms
        = new FilteredElementCollector( doc );

      rooms.WherePasses( new RoomFilter() );

      foreach( Room r in rooms )
      {
        sb.AppendFormat( "\r\n  Room {0}:'{1}': ",
          r.Id.ToString(), r.Name );

        if( r.Area > 0 ) // OK if having Area
        {
          sb.AppendFormat( "OK (A={0}[ft3])", r.Area );
        }
        else if( null == r.Location ) // Unplaced if no Location
        {
          sb.AppendFormat( "UnPlaced (Location is null)" );
        }
        else
        {
          sb.AppendFormat( "NotEnclosed or Redundant "
            + "- how to distinguish?" );
        }
      }
    }

    public enum RoomState
    {
      Unknown,
      Placed,
      NotPlaced,
      NotEnclosed,
      Redundant
    }

    /// <summary>
    /// Distinguish 'Not Placed',  'Redundant' 
    /// and 'Not Enclosed' rooms.
    /// </summary>
    RoomState DistinguishRoom( Room room )
    {
      RoomState res = RoomState.Unknown;

      if( room.Area > 0 )
      {
        // Placed if having Area

        res = RoomState.Placed;
      }
      else if( null == room.Location )
      {
        // No Area and No Location => Unplaced

        res = RoomState.NotPlaced;
      }
      else
      {
        // must be Redundant or NotEnclosed

        SpatialElementBoundaryOptions opt
          = new SpatialElementBoundaryOptions();

        IList<IList<BoundarySegment>> segs
          = room.GetBoundarySegments( opt );

        res = ( null == segs || segs.Count == 0 )
          ? RoomState.NotEnclosed
          : RoomState.Redundant;
      }
      return res;
    }

    /// <summary>
    /// Return a string for a bounding box
    /// which may potentially be null
    /// with its coordinates formatted to two
    /// decimal places.
    /// </summary>
    public static string BoundingBoxString2( BoundingBoxXYZ bb )
    {
      return null == bb
        ? "<null>"
        : Util.BoundingBoxString( bb );
    }

    /// <summary>
    /// List some properties of a given room to the
    /// Visual Studio debug output window.
    /// </summary>
    void ListRoomData( Room room )
    {
      SpatialElementBoundaryOptions opt
        = new SpatialElementBoundaryOptions();

      string nr = room.Number;
      string name = room.Name;
      double area = room.Area;

      Location loc = room.Location;
      LocationPoint lp = loc as LocationPoint;
      XYZ p = ( null == lp ) ? XYZ.Zero : lp.Point;

      BoundingBoxXYZ bb = room.get_BoundingBox( null );

      IList<IList<BoundarySegment>> boundary
        = room.GetBoundarySegments( opt );

      int nLoops = boundary.Count;

      int nFirstLoopSegments = 0 < nLoops
        ? boundary[0].Count
        : 0;

      Debug.Print( string.Format(
        "Room nr. '{0}' named '{1}' at {2} with "
        + "bounding box {3} and area {4} sqf has "
        + "{5} loop{6} and {7} segment{8} in first "
        + "loop.",
        nr, name, Util.PointString( p ),
        BoundingBoxString2( bb ), area, nLoops,
        Util.PluralSuffix( nLoops ), nFirstLoopSegments,
        Util.PluralSuffix( nFirstLoopSegments ) ) );
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      // Filtering for Room elements throws an exception:
      // Input type is of an element type that exists in
      // the API, but not in Revit's native object model.
      // Try using Autodesk.Revit.DB.SpatialElement
      // instead, and then postprocessing the results to
      // find the elements of interest.

      //FilteredElementCollector a
      //  = new FilteredElementCollector( doc )
      //    .OfClass( typeof( Room ) );

      // Solution using SpatialElement and then
      // checking for Room type

      //FilteredElementCollector a
      //  = new FilteredElementCollector( doc )
      //    .OfClass( typeof( SpatialElement ) );

      //foreach( SpatialElement e in a )
      //{
      //  Room room = e as Room;

      //  if( null != room )
      //  {
      //    ListRoomData( room );
      //  }
      //}

      // Improvement suggested by 
      // Victor Chekalin using LINQ

      // http://thebuildingcoder.typepad.com/blog/2011/11/accessing-room-data.html
      // ?cid=6a00e553e168978833017c3690489f970b#comment-6a00e553e168978833017c3690489f970b
      // --> version 2013.0.100.2

      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      var rooms = collector
        .OfClass( typeof( SpatialElement ) )
        .OfType<Room>();

      foreach( Room room in rooms )
      {
        ListRoomData( room );
      }

      return Result.Succeeded;
    }
  }
}

// C:\Program Files\Autodesk\Revit Architecture 2012\Program\Samples\rac_basic_sample_project.rvt
