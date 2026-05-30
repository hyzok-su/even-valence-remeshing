using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Plankton;
using PlanktonGh;
using System.Threading.Tasks;

/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
#region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { /* Implementation hidden. */ }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { /* Implementation hidden. */ }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { /* Implementation hidden. */ }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { /* Implementation hidden. */ }
#endregion

#region Members
  /// <summary>Gets the current Rhino document.</summary>
  private readonly RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private readonly GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private readonly IGH_Component Component;
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private readonly int Iteration;
#endregion

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments,
  /// Output parameters as ref arguments. You don't have to assign output parameters,
  /// they will have a default value.
  /// </summary>
  private void RunScript(Mesh mesh, double length, int iters, ref object A)
  {
    double PullStrength = 1;
    double SmoothStrength = 1;
    double LengthTol = 0.01;

    PlanktonMesh P = mesh.ToPlanktonMesh();
    Mesh M = mesh;
    for (int iter = 0; iter < iters; iter++)
    {
      int EdgeCount = P.Halfedges.Count / 2;
      double[] EdgeLength = P.Halfedges.GetLengths();
      List<bool> Visited = new List<bool>();
      Vector3d[] Normals = new Vector3d[P.Vertices.Count];
      for (int i = 0; i < P.Vertices.Count; i++)
      {
        Visited.Add(false);
        Normals[i] = Normal(P, i);
      }

      double t = LengthTol;     //a tolerance for when to split/collapse edges
      double smooth = SmoothStrength;  //smoothing strength
      double pull = PullStrength;  //pull to target mesh strength

      // Split the edges that are too long
      for (int i = 0; i < EdgeCount; i++)
      {
        if (P.Halfedges[2 * i].IsUnused == false)
        {
          int vStart = P.Halfedges[2 * i].StartVertex;
          int vEnd = P.Halfedges[2 * i + 1].StartVertex;

          if ((Visited[vStart] == false)
            && (Visited[vEnd] == false))
          {
            double L2 = length;

            if (EdgeLength[2 * i] > (1 + t) * (4f / 3f) * L2)
            {

              int SplitHEdge = P.Halfedges.TriangleSplitEdge(2 * i);
              if (SplitHEdge != -1)
              {
                int SplitCenter = P.Halfedges[SplitHEdge].StartVertex;
                //P.Vertices.SetVertex(SplitCenter, MidPt(P, i));
                Visited.Add(true);
              }
            }
          }
        }
      }

      //Collapse the edges that are too short
      for (int i = 0; i < EdgeCount; i++)
      {
        if (P.Halfedges[2 * i].IsUnused == false)
        {
          int vStart = P.Halfedges[2 * i].StartVertex;
          int vEnd = P.Halfedges[2 * i + 1].StartVertex;
          if ((Visited[vStart] == false)
            && (Visited[vEnd] == false))
          {
            double L2 = length;
            if (EdgeLength[2 * i] < (1 - t) * 4f / 5f * L2)
            {
              int Collapsed = -1;
              int CollapseRtn = -1;
              Collapsed = P.Halfedges[2 * i].StartVertex;
              P.Vertices.SetVertex(Collapsed, MidPt(P, i));
              CollapseRtn = P.Halfedges.CollapseEdge(2 * i);
              if (CollapseRtn != -1)
              {
                Visited[Collapsed] = true;
              }
            }
          }
        }
      }

      P.Compact(); //this cleans the mesh data structure of unused elements

      EdgeCount = P.Halfedges.Count / 2;
      if (PullStrength > 0)
      {
        //Flip edges to reduce valence error
        for (int i = 0; i < EdgeCount; i++)
        {
          if (!P.Halfedges[2 * i].IsUnused
            && (P.Halfedges[2 * i].AdjacentFace != -1)
            && (P.Halfedges[2 * i + 1].AdjacentFace != -1)
            )
          {
            int Vert1 = P.Halfedges[2 * i].StartVertex;
            int Vert2 = P.Halfedges[2 * i + 1].StartVertex;
            int Vert3 = P.Halfedges[P.Halfedges[P.Halfedges[2 * i].NextHalfedge].NextHalfedge].StartVertex;
            int Vert4 = P.Halfedges[P.Halfedges[P.Halfedges[2 * i + 1].NextHalfedge].NextHalfedge].StartVertex;

            int Valence1 = P.Vertices.GetValence(Vert1);
            int Valence2 = P.Vertices.GetValence(Vert2);
            int Valence3 = P.Vertices.GetValence(Vert3);
            int Valence4 = P.Vertices.GetValence(Vert4);

            if (P.Vertices.NakedEdgeCount(Vert1) > 0) { Valence1 += 2; }
            if (P.Vertices.NakedEdgeCount(Vert2) > 0) { Valence2 += 2; }
            if (P.Vertices.NakedEdgeCount(Vert3) > 0) { Valence3 += 2; }
            if (P.Vertices.NakedEdgeCount(Vert4) > 0) { Valence4 += 2; }

            int CurrentError =
              Math.Abs(Valence1 - 6) +
              Math.Abs(Valence2 - 6) +
              Math.Abs(Valence3 - 6) +
              Math.Abs(Valence4 - 6);
            int FlippedError =
              Math.Abs(Valence1 - 7) +
              Math.Abs(Valence2 - 7) +
              Math.Abs(Valence3 - 5) +
              Math.Abs(Valence4 - 5);

            int CurrentEvenError = Valence1 % 2 + Valence2 % 2 + Valence3 % 2 + Valence4 % 2;
            int FlippedEvenError = (Valence1 - 1) % 2 + (Valence2 - 1) % 2 + (Valence3 + 1) % 2 + (Valence4 + 1) % 2;

            if (CurrentError > FlippedError /*&& CurrentEvenError >= FlippedEvenError*/)
            {
              P.Halfedges.FlipEdge(2 * i);
            }
          }
        }
      }

      if(iter == iters - 1)
      {
        EVR(P);
        P.Compact();
      }

      Vector3d[] Smooth = LaplacianSmooth(P, 0, smooth);

      Parallel.For(0, P.Vertices.Count, i =>
        {
        // make it tangential only
        Vector3d VNormal = Normal(P, i);
        double ProjLength = Smooth[i] * VNormal;
        Smooth[i] = Smooth[i] - (VNormal * ProjLength);

        P.Vertices.MoveVertex(i, Smooth[i]);

        if (P.Vertices.NakedEdgeCount(i) != 0)//special smoothing for feature edges
        {
          int[] Neighbours = P.Vertices.GetVertexNeighbours(i);
          int ncount = 0;
          Point3d Avg = new Point3d();

          for (int j = 0; j < Neighbours.Length; j++)
          {
            if (P.Vertices.NakedEdgeCount(Neighbours[j]) != 0)
            {
              ncount++;
              Avg = Avg + P.Vertices[Neighbours[j]].ToPoint3d();
            }
          }
          Avg = Avg * (1.0 / ncount);
          Vector3d move = Avg - P.Vertices[i].ToPoint3d();
          move = move * smooth;
          P.Vertices.MoveVertex(i, move);
        }

        if (PullStrength > 0)
        {
          Point3d Point = P.Vertices[i].ToPoint3d();
          Vector3d normal = Normal(P, i);
          Ray3d Ray1 = new Ray3d(Point, normal);
          Ray3d Ray2 = new Ray3d(Point, -normal);
          double RayPt1 = Rhino.Geometry.Intersect.Intersection.MeshRay(M, Ray1);
          double RayPt2 = Rhino.Geometry.Intersect.Intersection.MeshRay(M, Ray2);
          Point3d ProjectedPt;

          if ((RayPt1 < RayPt2) && (RayPt1 > 0) && (RayPt1 < 1.0))
          {
            ProjectedPt = Point * (1 - pull) + pull * Ray1.PointAt(RayPt1);
          }
          else if ((RayPt2 < RayPt1) && (RayPt2 > 0) && (RayPt2 < 1.0))
          {
            ProjectedPt = Point * (1 - pull) + pull * Ray2.PointAt(RayPt2);
          }
          else
          {
            ProjectedPt = Point * (1 - pull) + pull * M.ClosestPoint(Point);
          }

          P.Vertices.SetVertex(i, ProjectedPt);
        }
        });

      P.Compact(); //this cleans the mesh data structure of unused elements

    }
    A = P;
  }

  // <Custom additional code> 
  private Vector3d Normal(PlanktonMesh P, int V)
  {
    Point3d Vertex = P.Vertices[V].ToPoint3d();
    Vector3d Norm = new Vector3d();

    int[] OutEdges = P.Vertices.GetHalfedges(V);
    int[] Neighbours = P.Vertices.GetVertexNeighbours(V);
    Vector3d[] OutVectors = new Vector3d[Neighbours.Length];
    int Valence = P.Vertices.GetValence(V);

    for (int j = 0; j < Valence; j++)
    {
      OutVectors[j] = P.Vertices[Neighbours[j]].ToPoint3d() - Vertex;
    }

    for (int j = 0; j < Valence; j++)
    {
      if (P.Halfedges[OutEdges[(j + 1) % Valence]].AdjacentFace != -1)
      {
        Norm += (Vector3d.CrossProduct(OutVectors[(j + 1) % Valence], OutVectors[j]));
      }
    }

    Norm.Unitize();
    return Norm;
  }

  private Point3d MidPt(PlanktonMesh P, int E)
  {
    Point3d Pos1 = P.Vertices[P.Halfedges[2 * E].StartVertex].ToPoint3d();
    Point3d Pos2 = P.Vertices[P.Halfedges[2 * E + 1].StartVertex].ToPoint3d();
    return (Pos1 + Pos2) * 0.5;
  }
  private static Vector3d[] LaplacianSmooth(PlanktonMesh P, int W, double Strength)
  {
    int VertCount = P.Vertices.Count;
    Vector3d[] Smooth = new Vector3d[VertCount];

    Parallel.For(0, VertCount, i =>
      {
      if ((P.Vertices[i].IsUnused == false) && (P.Vertices.IsBoundary(i) == false))
      {
        int[] Neighbours = P.Vertices.GetVertexNeighbours(i);
        Point3d Vertex = P.Vertices[i].ToPoint3d();
        Point3d Centroid = new Point3d();
        if (W == 0)
        {
          for (int j = 0; j < Neighbours.Length; j++)
          { Centroid = Centroid + P.Vertices[Neighbours[j]].ToPoint3d(); }
          Smooth[i] = ((Centroid * (1.0 / P.Vertices.GetValence(i))) - Vertex) * Strength;
        }
        if (W == 1)
        {
          //get the radial vectors of the 1-ring
          //get the vectors around the 1-ring
          //get the cotangent weights for each edge

          int valence = Neighbours.Length;

          Point3d[] NeighbourPts = new Point3d[valence];
          Vector3d[] Radial = new Vector3d[valence];
          Vector3d[] Around = new Vector3d[valence];
          double[] CotWeight = new double[valence];
          double WeightSum = 0;

          for (int j = 0; j < valence; j++)
          {
            NeighbourPts[j] = P.Vertices[Neighbours[j]].ToPoint3d();
            Radial[j] = NeighbourPts[j] - Vertex;
          }

          for (int j = 0; j < valence; j++)
          {
            Around[j] = NeighbourPts[(j + 1) % valence] - NeighbourPts[j];
          }

          for (int j = 0; j < Neighbours.Length; j++)
          {
            //get the cotangent weights
            int previous = (j + valence - 1) % valence;
            Vector3d Cross1 = Vector3d.CrossProduct(Radial[previous], Around[previous]);
            double Cross1Length = Cross1.Length;
            double Dot1 = Radial[previous] * Around[previous];

            int next = (j + 1) % valence;
            Vector3d Cross2 = Vector3d.CrossProduct(Radial[next], Around[j]);
            double Cross2Length = Cross2.Length;
            double Dot2 = Radial[next] * Around[j];

            CotWeight[j] = Math.Abs(Dot1 / Cross1Length) + Math.Abs(Dot2 / Cross2Length);
            WeightSum += CotWeight[j];
          }

          double InvWeightSum = 1.0 / WeightSum;

          Vector3d ThisSmooth = new Vector3d();

          for (int j = 0; j < Neighbours.Length; j++)
          {
            ThisSmooth = ThisSmooth + Radial[j] * CotWeight[j];
          }

          Smooth[i] = ThisSmooth * InvWeightSum * Strength;
        }

      }
      });
    return Smooth;
  }

  private int[] EdgePairPaths (PlanktonMesh p)
  {
    bool[] uneven = new bool[p.Vertices.Count];
    bool[] naked = new bool[p.Vertices.Count];
    List<int> vs = new List<int>();

    for(int i = 0;i < p.Vertices.Count;i++)
    {
      //Get Valence
      int he = p.Vertices[i].OutgoingHalfedge;
      int val = 0;
      foreach(int nh in p.Halfedges.GetVertexCirculator(he))
      {
        if(p.Halfedges[nh].AdjacentFace == -1){val = 0;break;}
        val++;
      }
      uneven[i] = val % 2 != 0;
      naked[i] = val == 0;
      if(uneven[i]) vs.Add(i);
    }
    if(vs.Count == 0) return new int[0];

    int[] uneven_vertices = vs.ToArray();
    bool[] ispaired = new bool[uneven_vertices.Length];
    List<int> edges = new List<int>();
    bool[] isinpath = new bool[p.Faces.Count];

    for(int i = 0;i < uneven_vertices.Length;i++)
    {
      if(!ispaired[i])
      {
        int[] queue = new int[p.Vertices.Count];
        bool[] inqueue = new bool[p.Vertices.Count];
        int quelast = -1;
        int quepointer = 0;
        int[] backrecords = new int[p.Vertices.Count];
        int[] edgerecords = new int[p.Vertices.Count];

        //start bfs
        quelast++;
        queue[quelast] = uneven_vertices[i];
        inqueue[uneven_vertices[i]] = true;
        while( quelast >= quepointer)
        {
          int vhere = queue[quepointer];
          quepointer++;

          foreach(int nh in p.Halfedges.GetVertexCirculator(p.Vertices[vhere].OutgoingHalfedge))
          {
            if(p.Halfedges[nh].AdjacentFace != -1
              && p.Halfedges[p.Halfedges.GetPairHalfedge(p.Halfedges[nh].NextHalfedge)].AdjacentFace != -1)
            {
              int nexth = p.Halfedges[nh].NextHalfedge;
              int nextv = p.Halfedges[p.Halfedges[p.Halfedges.GetPairHalfedge(nexth)].PrevHalfedge].StartVertex;

              int nextf1 = p.Halfedges[nh].AdjacentFace;
              int nextf2 = p.Halfedges[p.Halfedges.GetPairHalfedge(nexth)].AdjacentFace;
              if (!inqueue[nextv] && !isinpath[nextf1] && !isinpath[nextf2])
              {
                backrecords[nextv] = vhere;
                edgerecords[nextv] = nexth;
                if(uneven[nextv] || naked[nextv])
                {
                  int targetindex = vs.IndexOf(nextv);
                  if(targetindex != -1)
                  {
                    if(ispaired[targetindex] == true)
                    {
                      inqueue[nextv] = true;
                      quelast++;
                      queue[quelast] = nextv;
                    }
                    else
                    {
                      ispaired[i] = true;
                      ispaired[targetindex] = true;

                      int backptr = nextv;
                      while(backptr != uneven_vertices[i])
                      {
                        isinpath[p.Halfedges[edgerecords[backptr]].AdjacentFace] = true;
                        isinpath[p.Halfedges[p.Halfedges.GetPairHalfedge(edgerecords[backptr])].AdjacentFace] = true;
                        edges.Add(edgerecords[backptr]);
                        backptr = backrecords[backptr];
                      }
                      quepointer = 0;
                      quelast = -1;
                      break;
                    }
                  }
                  else
                  {
                    ispaired[i] = true;

                    int backptr = nextv;
                    while(backptr != uneven_vertices[i])
                    {
                      edges.Add(edgerecords[backptr]);
                      isinpath[p.Halfedges[edgerecords[backptr]].AdjacentFace] = true;
                      isinpath[p.Halfedges[p.Halfedges.GetPairHalfedge(edgerecords[backptr])].AdjacentFace] = true;
                      backptr = backrecords[backptr];
                    }
                    quepointer = 0;
                    quelast = -1;
                    break;
                  }
                }
                else
                {
                  inqueue[nextv] = true;
                  quelast++;
                  queue[quelast] = nextv;
                }
              }
            }
          }
        }
      }
    }
    return edges.ToArray();
  }

  private void EVR (PlanktonMesh p)
  {
    p.Compact();
    int[] edgePaths = EdgePairPaths(p);
    if(edgePaths.Length == 0)return;

    for(int i = 0;i < edgePaths.Length;i++)
    {
      int edge = edgePaths[i];

      var V1 = p.Vertices.GetValence(p.Halfedges[p.Halfedges[edge].PrevHalfedge].StartVertex);
      var V2 = p.Vertices.GetValence(p.Halfedges[p.Halfedges[p.Halfedges.GetPairHalfedge(edge)].PrevHalfedge].StartVertex);
      var V3 = p.Vertices.GetValence(p.Halfedges[edge].StartVertex);
      var V4 = p.Vertices.GetValence(p.Halfedges[p.Halfedges.GetPairHalfedge(edge)].StartVertex);
      bool collapse = ((V1 + V2 > 12 && V1 != 0 && V2 != 0) || (V1 == 0 && V2 > 6) || (V2 == 0 && V1 > 6)) && (V3 + V4 - 4 < 8);
      if(!collapse)
      {
        p.Halfedges.TriangleSplitEdge(edge);
      }

      else
      {
        int Collapsed = p.Halfedges[edge].StartVertex;
        p.Vertices.SetVertex(Collapsed, MidPt(p, edge / 2));
        p.Halfedges.CollapseEdge(edge);
      }
    }
  }
  // </Custom additional code> 
}
