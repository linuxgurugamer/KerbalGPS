/////////////////////////////////////////////////////////////////////////////////////////////
////
////   KerbalGPS_Math.cs
////
////   Kerbal Space Program GPS math library
////
////   (C) Copyright 2012-2013, Kevin Wilder (a.k.a. PakledHostage)
////
////   This code is licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 3.0) 
////   creative commons license. See <http://creativecommons.org/licenses/by-nc-sa/3.0/legalcode> 
////   for full details.
////
////   Attribution — You are free to modify this code, so long as you mention that the resulting
////                 work is based upon or adapted from this library. This KerbalGPS_Math.cs
////                 code library is the original work of Kevin Wilder.
////
////   Non-commercial - You may not use this work for commercial purposes.
////
////   Share Alike — If you alter, transform, or build upon this work, you may distribute the 
////                 resulting work only under the same or similar license to the CC BY-NC-SA 3.0
////                 license.
////
////
/////////////////////////////////////////////////////////////////////////////////////////////
////
////   Revision History
////
/////////////////////////////////////////////////////////////////////////////////////////////
////
////   Created November 10th, 2012
////
////   Revised December 17th, 2013
////
////    - Fixed a bug in 'Lon_to_String' function that emerged with the release of v0.23.0
////      of the game.
////
////
/////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbStar
{
    class GPS_Calculations
    {
        /////////////////////////////////////////////////////////////////////////////////////////////
        //
        //    Public Variables
        //
        /////////////////////////////////////////////////////////////////////////////////////////////

        private int[,] guCombinations = new int[7, 3] { { 0, 1, 2 }, { 1, 2, 3 }, { 0, 1, 3 }, { 0, 1, 4 }, { 1, 2, 4 }, { 1, 3, 4 }, { 2, 3, 4 } };
        private bool gyGPSCalculationsInitialised = false;
        private KerbStar.KerbalGPS clsLocalKerbalGPSReference = null;
        private float gfFilteredAltitude = 0.0f;
        private float gfFilteredError = 0.0f;
        private string strLat;
        private string strLon;
        private string strTime;
        private string strDistance;
        private string strHeading;
        private string strAcronym = NULL_ACRONYM;
        private string strSBAS = NULL_ACRONYM;
        public class SatPositions
        {
            public Vector3 position;
            public float distance;
            public SatPositions(Vector3 p, float d)
            {
                position = p;
                distance = d;
            }
        }
        private List<SatPositions> SatPositionsList = new List<SatPositions>();

        //private List<Vector3> gfSatellitePositions = new List<Vector3>();
        //private List<float> gfSatelliteDistances = new List<float>();


        /////////////////////////////////////////////////////////////////////////////////////////////
        //
        //    Private Variables
        //
        /////////////////////////////////////////////////////////////////////////////////////////////



        /////////////////////////////////////////////////////////////////////////////////////////////
        //
        //    Constants
        //
        /////////////////////////////////////////////////////////////////////////////////////////////

        private const float DEFAULT_GPS_SOLUTION_VALUE = 999999.9f;
        private const float MAXIMUM_SOLUTION_SCATTER = 10000.0f;
        private const float ALTITUDE_FINITE_FILTER_CONSTANT = 0.25f;
        private const float ERROR_FINITE_FILTER_CONSTANT = 0.25f;

        private const float RTD = 180.0f / Mathf.PI;
        private const float DTR = Mathf.PI / 180.0f;

        private const string NULL_ACRONYM = "NONE";


        /////////////////////////////////////////////////////////////////////////////////////////////
        //
        //    Implementation - Public functions
        //
        /////////////////////////////////////////////////////////////////////////////////////////////

        /********************************************************************************************
        Function Name: Initialise
        Parameters: see function definition
        Return: void

        Description:  Initialises the 'GPS_Calculations' class.

        *********************************************************************************************/

        public void Initialise(KerbStar.KerbalGPS clsKerbalGPSReference, string strGNSSacronym, string strSBASacronym)
        {
            if (!gyGPSCalculationsInitialised)
            {
                strAcronym = strGNSSacronym;
                strSBAS = strSBASacronym;
                gyGPSCalculationsInitialised = true;
                clsLocalKerbalGPSReference = clsKerbalGPSReference;
            }
        }


        /********************************************************************************************
        Function Name: Reset
        Parameters: see function definition
        Return: void

        Description:  Resets the 'GPS_Calculations' class.

        *********************************************************************************************/

        public void Reset()
        {
            strAcronym = NULL_ACRONYM;
            strSBAS = NULL_ACRONYM;
            gyGPSCalculationsInitialised = false;
            clsLocalKerbalGPSReference = null;
        }


        /********************************************************************************************
        Function Name: Calculate_GPS_Position
        Parameters: see function definition
        Return: see function definition

        Description:  Cacluates which GPS satellites are visible over the horizon and then uses those
                      satellites to compute a position solution.

        *********************************************************************************************/

        public bool Calculate_GPS_Position(out Vector3 fPosition, out UInt16 uNumSats, out float fPositionErrorEstimate, out float fFilteredAltitude)
        {
            List<Vector3> fGPSPositionSolutions = new List<Vector3>();

            Vector3 fSolution1 = new Vector3();
            Vector3 fSolution2 = new Vector3();
            Vector3 fKerbinPos = Vector3.zero;
            float fErrorEstimate = 0.0f;
            fPositionErrorEstimate = 0.0f;
            fFilteredAltitude = 0.0f;
            fPosition = Vector3.zero;

            UInt16 j;
            UInt16 uCombinations;
            bool yReturn = false;
            uNumSats = 0;

            if (gyGPSCalculationsInitialised)
            {
                // clear lists of satellite positions and distances
                SatPositionsList.Clear();
                //gfSatellitePositions.Clear();
                //gfSatelliteDistances.Clear();

                // Calculate spacecraft's position vector relative to current coordinate frame datum (used to calculate the relative position of GPS satellites)
                fPosition = clsLocalKerbalGPSReference.vessel.transform.worldToLocalMatrix * clsLocalKerbalGPSReference.vessel.CurrentCoM;

                // calculate which satellites are visble above the horizon (this requires knowledge of the vessel's altitude)
                fKerbinPos = clsLocalKerbalGPSReference.vessel.mainBody.position;
                fKerbinPos = clsLocalKerbalGPSReference.vessel.transform.worldToLocalMatrix * fKerbinPos;
                float fHorizonAngle = (Mathf.PI / 2.0f) - Mathf.Acos((float)(clsLocalKerbalGPSReference.vessel.mainBody.Radius / fKerbinPos.magnitude));

                for (int i = FlightGlobals.Vessels.Count - 1; i >= 0; i--)
                //foreach (Vessel varVessel in FlightGlobals.Vessels)
                {
                    Vessel varVessel = FlightGlobals.Vessels[i];
                    if (strAcronym == NULL_ACRONYM)
                    {
                        if (varVessel.isCommandable)
                        {
                            //for (int x = clsLocalKerbalGPSReference.GNSSSatelliteIDs.Count - 1; x >= 0; x--)
                            for (int x = clsLocalKerbalGPSReference.GNSSSatteliteInfoList.Count - 1; x >= 0; x--)
                            //foreach (Guid varGuid in clsLocalKerbalGPSReference.GNSSSatelliteIDs)
                            {
                                //Guid varGuid = clsLocalKerbalGPSReference.GNSSSatelliteIDs[x];
                                if (varVessel.id == clsLocalKerbalGPSReference.GNSSSatteliteInfoList[x].guid)
                                {

                                    Find_Distances_to_Satellites(varVessel, fKerbinPos, fPosition, fHorizonAngle, ref uNumSats, clsLocalKerbalGPSReference.GNSSSatteliteInfoList[x].gpsRange);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (varVessel.name.Contains(strAcronym) && varVessel.isCommandable)
                        {
                            for (int x = clsLocalKerbalGPSReference.GNSSSatteliteInfoList.Count - 1; x >= 0; x--)
                            {
                                if (varVessel.id == clsLocalKerbalGPSReference.GNSSSatteliteInfoList[x].guid)
                                {
                                    Find_Distances_to_Satellites(varVessel, fKerbinPos, fPosition, fHorizonAngle, ref uNumSats, clsLocalKerbalGPSReference.GNSSSatteliteInfoList[x].gpsRange);
                                    break;
                                }
                            }
                        }
                    }
                }

                // calculate GPS position using distances to visible satellites
                if (uNumSats < 4) uCombinations = 0;
                else if (uNumSats == 4) uCombinations = 3;
                else uCombinations = 7;

                if (uNumSats >= 4)
                {
                    for (j = 0; j < uCombinations; j++)
                    {
                        // Each set of three satellites defines up to two GPS position solutions. At most 1 of those is valid. 
                        //Trilateration(gfSatellitePositions, gfSatelliteDistances, out fSolution1, out fSolution2, j);
                        Trilateration(SatPositionsList, out fSolution1, out fSolution2, j);

                        fGPSPositionSolutions.Add(fSolution1);
                        fGPSPositionSolutions.Add(fSolution2);
                    }

                    // average GPS position solutions computed above
                    yReturn = Average_Solutions(fGPSPositionSolutions, out fPosition, out fErrorEstimate);

                    // transform calcualted position vector back into game's working coordinate frame
                    fPosition = clsLocalKerbalGPSReference.vessel.transform.localToWorldMatrix * fPosition;
                    fPosition += clsLocalKerbalGPSReference.vessel.CoMD;

                    // Calculate altitude from calculated position vector in game's working coordinate frame
                    gfFilteredAltitude += ((float)clsLocalKerbalGPSReference.vessel.mainBody.GetAltitude((Vector3d)fPosition) - gfFilteredAltitude) * ALTITUDE_FINITE_FILTER_CONSTANT;
                    fFilteredAltitude = gfFilteredAltitude;

                    gfFilteredError += (fErrorEstimate - gfFilteredError) * ERROR_FINITE_FILTER_CONSTANT;
                    fPositionErrorEstimate = gfFilteredError;

                }
                else
                {
                    fPosition = Vector3.zero;
                    fFilteredAltitude = 0.0f;
                    fPositionErrorEstimate = 0.0f;
                }
            }

            return yReturn;

        }



        /********************************************************************************************
        Function Name: FormatDegreesMinutesSeconds
        Parameters: see function definition
        Return: see function definition

        Description: Converts a degree into degree/minute/seconds string (or degree/minutes if that is desired)

        *********************************************************************************************/
        string FormatDegreesMinutesSeconds(float degrees, string NSEW)
        {
            float fMinutes;
            float fSec;
            string retStr;

            fMinutes = 60.0f * (degrees - Mathf.Floor(degrees));

            retStr = Mathf.Floor(degrees).ToString() + "° ";
            if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalGPSSettings>().useDecimalMinutes)
            {
                if (fMinutes < 10.0f)
                    retStr += "0" + Math.Round(fMinutes, 1).ToString("#0.0") + "' " + NSEW;
                else
                    retStr += Math.Round(fMinutes, 1).ToString("#0.0") + "' " + NSEW;
            }
            else
            {
                fSec = 60.0f * (fMinutes - Mathf.Floor(fMinutes));
                if (fMinutes < 10.0f)
                    retStr += "0" + Math.Floor(fMinutes).ToString() + "' ";
                else
                    retStr += Math.Floor(fMinutes).ToString() + "' ";

                if (fSec < 10.0f)
                    retStr += "0" + Math.Round(fSec, 1).ToString("#0.0") + "\" " + NSEW;
                else
                    retStr += Math.Round(fSec, 1).ToString("#0.0") + "\" " + NSEW;

            }
            return retStr;
        }

        /********************************************************************************************
        Function Name: Lat_to_String
        Parameters: see function definition
        Return: see function definition

        Description: Converts latitude into a string for display in the GUI

        *********************************************************************************************/

        public string Lat_to_String(float fLat)
        {
            //float fMinutes;
            
            string NS = "N";

            if (fLat < 0)
            {
                NS = "S";

                fLat *= -1.0f;
            }

            strLat = FormatDegreesMinutesSeconds(fLat, NS);

            //if (fMinutes < 10.0f) strLat = Mathf.Floor(fLat).ToString() + "° 0" + Math.Round(fMinutes, 1).ToString() + "' S";
            //else                  strLat = Mathf.Floor(fLat).ToString() + "° " + Math.Round(fMinutes, 1).ToString() + "' S";

            return strLat;
        }


        /********************************************************************************************
        Function Name: Lon_to_String
        Parameters: see function definition
        Return: see function definition

        Description: Converts longitude into a string for display in the GUI

        *********************************************************************************************/

        public string Lon_to_String(float fLon)
        {
            //float fMinutes;
            
            string EW = "E";

            if (fLon > 180.0f) fLon -= 360.0f;
            if (fLon < -180.0f) fLon += 360.0f;
            if (fLon < 0)
            {
                EW = "W";
                fLon *= -1.0f;
            }
            strLon = FormatDegreesMinutesSeconds(fLon, EW);
#if false
            fMinutes = 60.0f * (fLon - Mathf.Floor(fLon));

            strLat = Mathf.Floor(fLon).ToString() + "° ";


            if (fLon < 0)
            {
                fLon *= -1.0f;
                fMinutes = 60.0f * (fLon - Mathf.Floor(fLon));
                if (fMinutes < 10.0f) strLon = Mathf.Floor(fLon).ToString() + "° 0" + Math.Round(fMinutes, 1).ToString() + "' W";
                else strLon = Mathf.Floor(fLon).ToString() + "° " + Math.Round(fMinutes, 1).ToString() + "' W";
            }
            else
            {
                fMinutes = 60.0f * (fLon - Mathf.Floor(fLon));
                if (fMinutes < 10.0f) strLon = Mathf.Floor(fLon).ToString() + "° 0" + Math.Round(fMinutes, 1).ToString() + "' E";
                else strLon = Mathf.Floor(fLon).ToString() + "° " + Math.Round(fMinutes, 1).ToString() + "' E";
            }
#endif
            return strLon;
        }


        /********************************************************************************************
        Function Name: Time_to_String
        Parameters: see function definition
        Return: see function definition

        Description: Converts time (in seconds) into a text string

        *********************************************************************************************/

        public string Time_to_String(double dTime, bool yEarthTime)
        {
            double dDays, dHours, dMinutes;

            if (yEarthTime)
            {
                dDays = Math.Floor(dTime / 86400.0);
                dTime -= dDays * 86400.0;

                dHours = Math.Floor(dTime / 3600.0);
                dTime -= dHours * 3600.0;

                dMinutes = Math.Floor(dTime / 60.0);
                dTime -= dMinutes * 60.0;
            }
            else
            {
                dDays = 1.0 + Math.Floor(dTime / 21600.0);
                dTime -= dDays * 21600.0;

                dHours = Math.Floor(dTime / 3600.0);
                dTime -= dHours * 3600.0;

                dMinutes = Math.Floor(dTime / 60.0);
                dTime -= dMinutes * 60.0;
            }

            strTime = ((int)dDays).ToString() + " days " + ((int)dHours).ToString("D2") + ":" + ((int)dMinutes).ToString("D2") + ":" + ((int)dTime).ToString("D2");

            return strTime;
        }


        /********************************************************************************************
        Function Name: Great_Circle_Distance
        Parameters: see function definition
        Return: string - Distance

        Description: Calculates the great circle distance (i.e. shortest distance) between the two 
        given positions on the sphere. Converts the result into a string, which it returns.

        *********************************************************************************************/

        public string Great_Circle_Distance(float fOLat, float fOLon, float fDLat, float fDLon, float fAlt)
        {
            float fDistance;

            // Calculate position vectors for both the origin and destination
            Vector3d gdOrig = new Vector3d(Mathf.Cos(DTR * fOLat) * Mathf.Cos(DTR * fOLon), Mathf.Cos(DTR * fOLat) * Mathf.Sin(DTR * fOLon), Mathf.Sin(DTR * fOLat));
            Vector3d gdDest = new Vector3d(Mathf.Cos(DTR * fDLat) * Mathf.Cos(DTR * fDLon), Mathf.Cos(DTR * fDLat) * Mathf.Sin(DTR * fDLon), Mathf.Sin(DTR * fDLat));

            // Normalise both vectors and trap rounding errors that result in |fDot| > 1.0
            gdOrig = gdOrig.normalized;
            gdDest = gdDest.normalized;
            double dDot = Vector3d.Dot(gdOrig, gdDest);
            if (dDot > 1.0f) dDot = 1.0f;
            if (dDot < -1.0f) dDot = -1.0f;

            // great circle distance is simply the angle subtended by the two vectors in radians times the radius of the sphere
            fDistance = (float)(clsLocalKerbalGPSReference.vessel.mainBody.Radius * Math.Acos(dDot));

            if (fDistance < 1000.0f)
                strDistance = (fDistance.ToString("#0.0")) + " m";
            else if (fDistance < 10000.0f)
                strDistance = (Mathf.RoundToInt(fDistance).ToString("#0")) + " m";
            else if (fDistance < 100000.0f)
                strDistance = ((fDistance / 1000.0f).ToString("#0.00")) + " km";
            else
                strDistance = ((fDistance / 1000.0f).ToString("#0.0")) + " km";

            return strDistance;
        }


        /********************************************************************************************
        Function Name: Great_Circle_Heading
        Parameters: void
        Return: string - Heading

        Description: Calculates the heading along the great circle, from the current position to the 
        destination.

        *********************************************************************************************/

        public string Great_Circle_Heading(float fOLat, float fOLon, float fDLat, float fDLon)
        {
            float fTrueHeading;

            // convert lat and lon into radians
            fDLat *= DTR;
            fDLon *= DTR;
            fOLat *= DTR;
            fOLon *= DTR;

            // calculate x and y component of heading vector
            float fY = Mathf.Sin(fDLon - fOLon) * Mathf.Cos(fDLat);
            float fX = Mathf.Cos(fOLat) * Mathf.Sin(fDLat) - Mathf.Sin(fOLat) * Mathf.Cos(fDLat) * Mathf.Cos(fDLon - fOLon);

            // calculate heading
            if ((fX == 0.0f) && (fY == 0.0f))
                fTrueHeading = 0.0f;
            else
                fTrueHeading = RTD * Mathf.Atan2(fY, fX);

            // convert heading into compass heading
            fTrueHeading = (fTrueHeading + 360.0f) % 360.0f;

            // convert compass heading into a string
            strHeading = Mathf.RoundToInt(fTrueHeading).ToString("#0") + "°";

            return strHeading;
        }


        /////////////////////////////////////////////////////////////////////////////////////////////
        //
        //    Implementation - Private functions
        //
        /////////////////////////////////////////////////////////////////////////////////////////////

        /********************************************************************************************
        Function Name: Trilateration
        Parameters: see function definition
        Return: void

        Description:  Calcuates GPS position solutions using trilateration. Solves for the 3D 
                      position by rotating the plane that contains the solution into the XY plane
                      and then solving for the 2D position. Finally, it rotates the solution vector
                      back into the original coordinate frame.

        *********************************************************************************************/

        //private void Trilateration(List<Vector3> fSatellitePositions, List<float> fSatelliteDistances, out Vector3 fSolution1, out Vector3 fSolution2, int j)
        private void Trilateration(List<SatPositions> satPositionList, out Vector3 fSolution1, out Vector3 fSolution2, int j)
        {

            fSolution1 = DEFAULT_GPS_SOLUTION_VALUE * Vector3.up;
            fSolution2 = DEFAULT_GPS_SOLUTION_VALUE * Vector3.up;

            // transform solution space into coordinate frame centred on the first satellite in the set of three
            //Vector3 vectP2 = fSatellitePositions[guCombinations[j, 1]] - fSatellitePositions[guCombinations[j, 0]];
            //Vector3 vectP3 = fSatellitePositions[guCombinations[j, 2]] - fSatellitePositions[guCombinations[j, 0]];

            //float fR1 = fSatelliteDistances[guCombinations[j, 0]];
            //float fR2 = fSatelliteDistances[guCombinations[j, 1]];
            //float fR3 = fSatelliteDistances[guCombinations[j, 2]];

            Vector3 vectP2 = satPositionList[guCombinations[j, 1]].position - satPositionList[guCombinations[j, 0]].position;
            Vector3 vectP3 = satPositionList[guCombinations[j, 2]].position - satPositionList[guCombinations[j, 0]].position;

            float fR1 = satPositionList[guCombinations[j, 0]].distance;
            float fR2 = satPositionList[guCombinations[j, 1]].distance;
            float fR3 = satPositionList[guCombinations[j, 2]].distance;


            float fD = vectP2.magnitude;
            float fZsquared;

            Vector3 vectNormal = Vector3.Cross(vectP2, vectP3);

            if (vectNormal != Vector3.zero)
            {
                // define orthogonal unit vectors X', Y' and Z' such that satellite 0 is at the origin, satellite 1 is on the X' axis and satellite 2 is in the X'Y' plane. 
                vectNormal = vectNormal.normalized;
                Vector3 vectAbscissa = vectP2.normalized;
                Matrix4x4 matBasis = new Matrix4x4();

                // define a rotation matrix in terms of those unit vectors to rotate the X', Y', Z' coordinate frame into the X, Y, Z coordinate frame
                matBasis.SetRow(0, vectAbscissa);
                matBasis.SetRow(1, Vector3.Cross(vectNormal, vectAbscissa));
                matBasis.SetRow(2, vectNormal);

                // rotate position vectors into the XY plane
                vectP2 = matBasis * vectP2;
                vectP3 = matBasis * vectP3;

                // compute 2-D trilateration
                fSolution1[0] = (Mathf.Pow(fR1, 2.0f) - Mathf.Pow(fR2, 2.0f) + Mathf.Pow(fD, 2.0f)) / (2.0f * fD);
                fSolution1[1] = (Mathf.Pow(fR1, 2.0f) - Mathf.Pow(fR3, 2.0f) + Mathf.Pow(vectP3[0], 2.0f) + Mathf.Pow(vectP3[1], 2.0f)) / (2.0f * vectP3[1]) - ((vectP3[0] / vectP3[1]) * fSolution1[0]);
                fZsquared = Mathf.Pow(fR1, 2.0f) - Mathf.Pow(fSolution1[0], 2.0f) - Mathf.Pow(fSolution1[1], 2.0f);

                if (fZsquared >= 0.0f)
                {
                    fSolution2[0] = fSolution1[0];
                    fSolution2[1] = fSolution1[1];
                    fSolution1[2] = Mathf.Pow(fZsquared, 0.5f);
                    fSolution2[2] = -fSolution1[2];

                    // rotate plane containing solution back into original X', Y', Z' coordinate frame and transform solution vector into spacecraft centric coordinates
                    fSolution1 = matBasis.transpose * fSolution1;
                    fSolution2 = matBasis.transpose * fSolution2;
                    //fSolution1 = fSolution1 + fSatellitePositions[guCombinations[j, 0]];
                    //fSolution2 = fSolution2 + fSatellitePositions[guCombinations[j, 0]];
                    fSolution1 = fSolution1 + satPositionList[guCombinations[j, 0]].position;
                    fSolution2 = fSolution2 + satPositionList[guCombinations[j, 0]].position;



                }
                else
                {
                    // reset first solution to defult if no solution exists
                    fSolution1 = DEFAULT_GPS_SOLUTION_VALUE * Vector3.up;
                }

            }
        }


        /********************************************************************************************
        Function Name: Find_Distances_to_Satellites
        Parameters: See function definition
        Return: void

        Description: Adds location and distance to GNSS satellites to lists

        *********************************************************************************************/

        private void Find_Distances_to_Satellites(Vessel varVessel, Vector3 fKerbinPos, Vector3 fPosition, float fHorizonAngle, ref ushort uNumSats, double gpsRange)
        {
            // Retrieve GPS satellite position and transform into vessel centric coordinate frame
            Vector3 fGPSSatellitePosition = clsLocalKerbalGPSReference.vessel.transform.worldToLocalMatrix * varVessel.CurrentCoM;
            fGPSSatellitePosition -= fPosition;

            // Calculate whether or not satellite is visible above the horizon. If so, add it to the list of satellites used in the position fix calculation
            if (Cosine_Law(fGPSSatellitePosition, fKerbinPos) > fHorizonAngle)
            {
                float fDistance = (fGPSSatellitePosition).magnitude;
                if (fDistance < clsLocalKerbalGPSReference.vessel.mainBody.sphereOfInfluence && (HighLogic.CurrentGame.Parameters.CustomParams<KerbalGPSSettings>().useGPSrange && fDistance <= gpsRange))
                {
                    SatPositionsList.Add(new SatPositions(fGPSSatellitePosition, fDistance));
                    //gfSatellitePositions.Add(fGPSSatellitePosition);
                    //gfSatelliteDistances.Add(fDistance);
                    uNumSats++;
                }
            }
        }


        /********************************************************************************************
        Function Name: Average_Solutions
        Parameters: List of GPS position solutions from trilateration function
        Return: Least squares solution from list of computed solutions

        Description: Computes the average GPS position solution from the valid position fixes.

        Note: This function would need some more work if I wanted to be more rigorous... it uses  
              information that wouldn't be available if this wasn't being done in a game.

        *********************************************************************************************/

        private bool Average_Solutions(List<Vector3> fPositions, out Vector3 fVectorAverage, out float fErrorAverage)
        {
            int j;
            int uIndex = 0;
            float fMagnitude;
            fErrorAverage = 0.0f;
            fVectorAverage = Vector3.zero;

            for (j = 0; j < (fPositions.Count); j++)
            {
                // Cheesy averaging of solutions that are within MAXIMUM_SOLUTION_SCATTER from the known position
                // A more rigorous method would involve determining which solutions are closest to each other and averaging those,
                // rather than using the known position as a reference.
                fMagnitude = fPositions[j].magnitude;
                if (fMagnitude < MAXIMUM_SOLUTION_SCATTER)
                {
                    uIndex++;
                    fVectorAverage += fPositions[j];
                    fErrorAverage += fMagnitude;
                }
            }

            if (uIndex > 0)
            {
                fVectorAverage = fVectorAverage / ((float)uIndex);
                fErrorAverage /= ((float)uIndex);
                return true;
            }
            else
            {
                return false;
            }
        }


        /********************************************************************************************
        Function Name: Cosine_Law
        Parameters: See function definition
        Return: float - Angle between two vectors passed as parameters

        Description: Uses the Cosine Law to calculate the angular distance between two vectors in R3.

        Note: This function was developed because the built-in Unity3D "Angle" function returned  
              results that are inconsistent with the expected values.

        *********************************************************************************************/

        private float Cosine_Law(Vector3 fVectorFrom, Vector3 fVectorTo)
        {
            float fFromMag = fVectorFrom.magnitude;
            float fToMag = fVectorTo.magnitude;

            if ((fFromMag > 0.0f) && (fToMag > 0.0f))
                return Mathf.Acos(Vector3.Dot(fVectorFrom, fVectorTo) / (fVectorFrom.magnitude * fVectorTo.magnitude));
            else
                return 0.0f;
        }

    }
}
//
// END OF FILE
//

