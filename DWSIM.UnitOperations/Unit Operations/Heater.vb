﻿'    Heater Calculation Routines 
'    Copyright 2008 Daniel Wagner O. de Medeiros
'
'    This file is part of DWSIM.
'
'    DWSIM is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    DWSIM is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with DWSIM.  If not, see <http://www.gnu.org/licenses/>.

Imports DWSIM.DrawingTools.GraphicObjects
Imports DWSIM.Thermodynamics
Imports DWSIM.Thermodynamics.Streams
Imports DWSIM.SharedClasses
Imports System.Windows.Forms
Imports DWSIM.UnitOperations.UnitOperations.Auxiliary
Imports DWSIM.Thermodynamics.BaseClasses
Imports DWSIM.Interfaces.Enums

Namespace UnitOperations

    <System.Serializable()> Public Class Heater

        Inherits UnitOperations.UnitOpBaseClass

        <NonSerialized> <Xml.Serialization.XmlIgnore> Dim f As EditingForm_HeaterCooler

        Public Enum CalculationMode
            HeatAdded = 0
            OutletTemperature = 1
            EnergyStream = 2
            OutletVaporFraction = 3
            TemperatureChange = 4
        End Enum

        Protected m_dp As Nullable(Of Double)
        Protected m_dt As Nullable(Of Double)
        Protected m_DQ As Nullable(Of Double)
        Protected m_Tout As Nullable(Of Double) = 298.15#
        Protected m_VFout As Nullable(Of Double)
        Protected m_cmode As CalculationMode = CalculationMode.HeatAdded

        Protected m_eta As Nullable(Of Double) = 100

        Protected m_FixOnHeat As Boolean = True

        Public Property FixOnHeat() As Boolean
            Get
                Return m_FixOnHeat
            End Get
            Set(ByVal value As Boolean)
                m_FixOnHeat = value
            End Set
        End Property

        Public Sub New(ByVal name As String, ByVal description As String)
            MyBase.CreateNew()
            Me.ComponentName = name
            Me.ComponentDescription = description

        End Sub

        Public Property Eficiencia() As Nullable(Of Double)
            Get
                Return m_eta
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_eta = value
            End Set
        End Property

        Public Property OutletVaporFraction() As Nullable(Of Double)
            Get
                Return m_VFout
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_VFout = value
            End Set
        End Property

        Public Property OutletTemperature() As Nullable(Of Double)
            Get
                Return m_Tout
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_Tout = value
            End Set
        End Property

        Public Property CalcMode() As CalculationMode
            Get
                Return m_cmode
            End Get
            Set(ByVal value As CalculationMode)
                m_cmode = value
            End Set
        End Property


        Public Property DeltaP() As Nullable(Of Double)
            Get
                Return m_dp
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_dp = value
            End Set
        End Property

        Public Property DeltaT() As Nullable(Of Double)
            Get
                Return m_dt
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_dt = value
            End Set
        End Property

        Public Property DeltaQ() As Nullable(Of Double)
            Get
                Return m_DQ
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_DQ = value
            End Set
        End Property

        Public Sub New()
            MyBase.New()
        End Sub

        Public Overrides Function CloneXML() As Object
            Return New Heater().LoadData(Me.SaveData)
        End Function

        Public Overrides Function CloneJSON() As Object
            Return Newtonsoft.Json.JsonConvert.DeserializeObject(Of Heater)(Newtonsoft.Json.JsonConvert.SerializeObject(Me))
        End Function

        Public Overrides Sub Calculate(Optional ByVal args As Object = Nothing)

            If Not Me.GraphicObject.InputConnectors(1).IsAttached Then
                Throw New Exception(FlowSheet.GetTranslatedString("NohcorrentedeEnergyFlow2"))
            ElseIf Not Me.GraphicObject.OutputConnectors(0).IsAttached Then
                Throw New Exception(FlowSheet.GetTranslatedString("Verifiqueasconexesdo"))
            ElseIf Not Me.GraphicObject.InputConnectors(0).IsAttached Then
                Throw New Exception(FlowSheet.GetTranslatedString("Verifiqueasconexesdo"))
            End If

            Dim Ti, Pi, Hi, Wi, ei, ein, T2, P2, H2, V2 As Double

            Dim msin, msout As MaterialStream, esin As Streams.EnergyStream

            msin = FlowSheet.SimulationObjects(Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Name)
            msout = FlowSheet.SimulationObjects(Me.GraphicObject.OutputConnectors(0).AttachedConnector.AttachedTo.Name)
            esin = FlowSheet.SimulationObjects(Me.GraphicObject.InputConnectors(1).AttachedConnector.AttachedFrom.Name)

            msin.Validate()

            Me.PropertyPackage.CurrentMaterialStream = msin
            Ti = msin.Phases(0).Properties.temperature.GetValueOrDefault
            Pi = msin.Phases(0).Properties.pressure.GetValueOrDefault
            Hi = msin.Phases(0).Properties.enthalpy.GetValueOrDefault
            Wi = msin.Phases(0).Properties.massflow.GetValueOrDefault
            ei = Hi * Wi
            ein = ei

            P2 = Pi - Me.DeltaP.GetValueOrDefault

            If DebugMode Then AppendDebugLine(String.Format("Property Package: {0}", Me.PropertyPackage.Name))
            If DebugMode Then AppendDebugLine(String.Format("Input variables: T = {0} K, P = {1} Pa, H = {2} kJ/kg, W = {3} kg/s", Ti, Pi, Hi, Wi))

            If DebugMode Then AppendDebugLine("Calculation mode: " & CalcMode.ToString)

            Select Case Me.CalcMode

                Case CalculationMode.HeatAdded

                    H2 = Me.DeltaQ.GetValueOrDefault * (Me.Eficiencia.GetValueOrDefault / 100) / Wi + Hi

                    If DebugMode Then AppendDebugLine(String.Format("Doing a PH flash to calculate outlet temperature... P = {0} Pa, H = {1} kJ/[kg.K]", P2, H2))

                    Dim tmp = Me.PropertyPackage.CalculateEquilibrium2(FlashCalculationType.PressureEnthalpy, P2, H2, 0)
                    T2 = tmp.CalculatedTemperature
                    Me.DeltaT = T2 - Ti

                    OutletTemperature = T2

                    If DebugMode Then AppendDebugLine(String.Format("Calculated outlet temperature T2 = {0} K", T2))

                    'Corrente de EnergyFlow - atualizar valor da potencia (kJ/s)
                    With esin
                        .EnergyFlow = Me.DeltaQ.GetValueOrDefault
                        .GraphicObject.Calculated = True
                    End With

                Case CalculationMode.OutletTemperature

                    T2 = Me.OutletTemperature.GetValueOrDefault

                    If DebugMode Then AppendDebugLine(String.Format("Doing a PT flash to calculate outlet enthalpy... P = {0} Pa, T = {1} K", P2, T2))

                    Dim tmp = Me.PropertyPackage.CalculateEquilibrium2(FlashCalculationType.PressureTemperature, P2, T2, 0)
                    H2 = tmp.CalculatedEnthalpy
                    CheckSpec(H2, False, "outlet enthalpy")
                    Me.DeltaT = T2 - Ti
                    Me.DeltaQ = (H2 - Hi) / (Me.Eficiencia.GetValueOrDefault / 100) * Wi

                    'Corrente de EnergyFlow - atualizar valor da potencia (kJ/s)
                    With esin
                        .EnergyFlow = Me.DeltaQ.GetValueOrDefault
                        .GraphicObject.Calculated = True
                    End With

                Case CalculationMode.TemperatureChange

                    T2 = Ti + Me.DeltaT.GetValueOrDefault

                    OutletTemperature = T2

                    If DebugMode Then AppendDebugLine(String.Format("Doing a PT flash to calculate outlet enthalpy... P = {0} Pa, T = {1} K", P2, T2))

                    Dim tmp = Me.PropertyPackage.CalculateEquilibrium2(FlashCalculationType.PressureTemperature, P2, T2, 0)
                    H2 = tmp.CalculatedEnthalpy
                    CheckSpec(H2, False, "outlet enthalpy")
                    Me.DeltaQ = (H2 - Hi) / (Me.Eficiencia.GetValueOrDefault / 100) * Wi

                    'Corrente de EnergyFlow - atualizar valor da potencia (kJ/s)
                    With esin
                        .EnergyFlow = Me.DeltaQ.GetValueOrDefault
                        .GraphicObject.Calculated = True
                    End With

                Case CalculationMode.EnergyStream

                    Me.DeltaQ = esin.EnergyFlow.GetValueOrDefault
                    H2 = Me.DeltaQ.GetValueOrDefault * (Me.Eficiencia.GetValueOrDefault / 100) / Wi + Hi

                    If DebugMode Then AppendDebugLine(String.Format("Doing a PH flash to calculate outlet temperature... P = {0} Pa, H = {1} kJ/[kg.K]", P2, H2))

                    Dim tmp = Me.PropertyPackage.CalculateEquilibrium2(FlashCalculationType.PressureEnthalpy, P2, H2, Ti)
                    T2 = tmp.CalculatedTemperature
                    CheckSpec(T2, True, "outlet temperature")
                    Me.DeltaT = T2 - Ti

                    OutletTemperature = T2

                    If DebugMode Then AppendDebugLine(String.Format("Calculated outlet temperature T2 = {0} K", T2))

                Case CalculationMode.OutletVaporFraction

                    V2 = m_VFout.GetValueOrDefault

                    If DebugMode Then AppendDebugLine(String.Format("Doing a PVF flash to calculate outlet temperature... P = {0} Pa, VF = {1}", P2, V2))

                    Dim tmp = Me.PropertyPackage.CalculateEquilibrium2(FlashCalculationType.PressureVaporFraction, P2, m_VFout.GetValueOrDefault, Ti)
                    H2 = tmp.CalculatedEnthalpy
                    CheckSpec(H2, False, "outlet enthalpy")
                    T2 = tmp.CalculatedTemperature
                    CheckSpec(T2, True, "outlet temperature")
                    Me.DeltaT = T2 - Ti
                    Me.DeltaQ = (H2 - Hi) / (Me.Eficiencia.GetValueOrDefault / 100) * Wi
                    OutletTemperature = T2

                    If DebugMode Then AppendDebugLine(String.Format("Calculated outlet temperature T2 = {0} K", T2))

                    'Corrente de EnergyFlow - atualizar valor da potencia (kJ/s)
                    With esin
                        .EnergyFlow = Me.DeltaQ.GetValueOrDefault
                        .GraphicObject.Calculated = True
                    End With

            End Select

            If Not DebugMode Then

                'Atribuir valores a corrente de materia conectada a jusante
                Dim omstr As MaterialStream = FlowSheet.SimulationObjects(Me.GraphicObject.OutputConnectors(0).AttachedConnector.AttachedTo.Name)
                With omstr
                    .Phases(0).Properties.temperature = T2
                    .Phases(0).Properties.pressure = P2
                    .Phases(0).Properties.enthalpy = H2
                    .Phases(2).Properties.molarfraction = V2
                    Dim comp As BaseClasses.Compound
                    For Each comp In .Phases(0).Compounds.Values
                        comp.MoleFraction = msin.Phases(0).Compounds(comp.Name).MoleFraction
                        comp.MassFraction = msin.Phases(0).Compounds(comp.Name).MassFraction
                    Next
                    .Phases(0).Properties.massflow = msin.Phases(0).Properties.massflow.GetValueOrDefault
                End With

            Else

                AppendDebugLine("Calculation finished successfully.")

            End If

        End Sub

        Public Overrides Sub DeCalculate()

            If Me.GraphicObject.OutputConnectors(0).IsAttached Then

                'Zerar valores da corrente de materia conectada a jusante
                With DirectCast(FlowSheet.SimulationObjects(Me.GraphicObject.OutputConnectors(0).AttachedConnector.AttachedTo.Name), MaterialStream)
                    .Phases(0).Properties.temperature = Nothing
                    .Phases(0).Properties.pressure = Nothing
                    .Phases(0).Properties.enthalpy = Nothing
                    .Phases(0).Properties.molarfraction = 1
                    .Phases(0).Properties.massfraction = 1
                    Dim comp As BaseClasses.Compound
                    Dim i As Integer = 0
                    For Each comp In .Phases(0).Compounds.Values
                        comp.MoleFraction = 0
                        comp.MassFraction = 0
                        i += 1
                    Next
                    .Phases(0).Properties.massflow = Nothing
                    .Phases(0).Properties.molarflow = Nothing
                    .GraphicObject.Calculated = False
                End With

            End If

            'Corrente de EnergyFlow - atualizar valor da potencia (kJ/s)
            If Me.GraphicObject.EnergyConnector.IsAttached Then
                With DirectCast(FlowSheet.SimulationObjects(Me.GraphicObject.EnergyConnector.AttachedConnector.AttachedTo.Name), Streams.EnergyStream)
                    .EnergyFlow = Nothing
                    .GraphicObject.Calculated = False
                End With
            End If

        End Sub

        Public Overrides Function GetPropertyValue(ByVal prop As String, Optional ByVal su As Interfaces.IUnitsOfMeasure = Nothing) As Object

            Dim val0 As Object = MyBase.GetPropertyValue(prop, su)

            If Not val0 Is Nothing Then
                Return val0
            Else


                If su Is Nothing Then su = New SystemsOfUnits.SI
                Dim cv As New SystemsOfUnits.Converter
                Dim value As Double = 0
                Dim propidx As Integer = Convert.ToInt32(prop.Split("_")(2))

                Select Case propidx

                    Case 0
                        'PROP_HT_0	Pressure Drop
                        value = SystemsOfUnits.Converter.ConvertFromSI(su.deltaP, Me.DeltaP.GetValueOrDefault)
                    Case 1
                        'PROP_HT_1(Efficiency)
                        value = Me.Eficiencia.GetValueOrDefault
                    Case 2
                        'PROP_HT_2	Outlet Temperature
                        value = SystemsOfUnits.Converter.ConvertFromSI(su.temperature, Me.OutletTemperature.GetValueOrDefault)
                    Case 3
                        'PROP_HT_3	Heat Added
                        value = SystemsOfUnits.Converter.ConvertFromSI(su.heatflow, Me.DeltaQ.GetValueOrDefault)
                    Case 4
                        'PROP_HT_4 Outlet molar vapour fraction
                        value = Me.OutletVaporFraction.GetValueOrDefault
                    Case 5
                        'PROP_HT_5 DeltaT
                        value = SystemsOfUnits.Converter.ConvertFromSI(su.deltaT, Me.DeltaT.GetValueOrDefault)
                End Select

                Return value
            End If

        End Function

        Public Overloads Overrides Function GetProperties(ByVal proptype As Interfaces.Enums.PropertyType) As String()
            Dim i As Integer = 0
            Dim proplist As New ArrayList
            Dim basecol = MyBase.GetProperties(proptype)
            If basecol.Length > 0 Then proplist.AddRange(basecol)
            Select Case proptype
                Case PropertyType.RO
                    For i = 5 To 5
                        proplist.Add("PROP_HT_" + CStr(i))
                    Next
                Case PropertyType.RW
                    For i = 0 To 5
                        proplist.Add("PROP_HT_" + CStr(i))
                    Next
                Case PropertyType.WR
                    For i = 0 To 4
                        proplist.Add("PROP_HT_" + CStr(i))
                    Next
                Case PropertyType.ALL
                    For i = 0 To 5
                        proplist.Add("PROP_HT_" + CStr(i))
                    Next
            End Select
            Return proplist.ToArray(GetType(System.String))
            proplist = Nothing
        End Function

        Public Overrides Function SetPropertyValue(ByVal prop As String, ByVal propval As Object, Optional ByVal su As Interfaces.IUnitsOfMeasure = Nothing) As Boolean

            If MyBase.SetPropertyValue(prop, propval, su) Then Return True

            If su Is Nothing Then su = New SystemsOfUnits.SI
            Dim cv As New SystemsOfUnits.Converter
            Dim propidx As Integer = Convert.ToInt32(prop.Split("_")(2))

            Select Case propidx

                Case 0
                    'PROP_HT_0	Pressure Drop
                    Me.DeltaP = SystemsOfUnits.Converter.ConvertToSI(su.deltaP, propval)
                Case 1
                    'PROP_HT_1(Efficiency)
                    Me.Eficiencia = propval
                Case 2
                    'PROP_HT_2	Outlet Temperature
                    Me.OutletTemperature = SystemsOfUnits.Converter.ConvertToSI(su.temperature, propval)
                Case 3
                    'PROP_HT_3	Heat Added
                    Me.DeltaQ = SystemsOfUnits.Converter.ConvertToSI(su.heatflow, propval)
                Case 4
                    'PROP_HT_3	Outlet molar vapour fraction
                    Me.OutletVaporFraction = propval
            End Select
            Return 1
        End Function

        Public Overrides Function GetPropertyUnit(ByVal prop As String, Optional ByVal su As Interfaces.IUnitsOfMeasure = Nothing) As String
            Dim u0 As String = MyBase.GetPropertyUnit(prop, su)

            If u0 <> "NF" Then
                Return u0
            Else
                If su Is Nothing Then su = New SystemsOfUnits.SI
                Dim cv As New SystemsOfUnits.Converter
                Dim value As String = ""
                Dim propidx As Integer = Convert.ToInt32(prop.Split("_")(2))

                Select Case propidx

                    Case 0
                        'PROP_HT_0	Pressure Drop
                        value = su.deltaP
                    Case 1
                        'PROP_HT_1(Efficiency)
                        value = ""
                    Case 2
                        'PROP_HT_2	Outlet Temperature
                        value = su.temperature
                    Case 3
                        'PROP_HT_3	Heat Added
                        value = su.heatflow
                    Case 4
                        'PROP_HT_4	Outlet vapour fraction
                        value = ""
                    Case 5
                        'PROP_HT_5 DeltaT
                        value = su.deltaT

                End Select

                Return value
            End If
        End Function

        Public Overrides Sub DisplayEditForm()

            If f Is Nothing Then
                f = New EditingForm_HeaterCooler With {.SimObject = Me}
                f.ShowHint = GlobalSettings.Settings.DefaultEditFormLocation
                Me.FlowSheet.DisplayForm(f)
            Else
                If f.IsDisposed Then
                    f = New EditingForm_HeaterCooler With {.SimObject = Me}
                    f.ShowHint = GlobalSettings.Settings.DefaultEditFormLocation
                    Me.FlowSheet.DisplayForm(f)
                Else
                    f.Activate()
                End If
            End If

        End Sub

        Public Overrides Sub UpdateEditForm()
            If f IsNot Nothing Then
                If Not f.IsDisposed Then
                    f.UIThread(Sub() f.UpdateInfo())
                End If
            End If
        End Sub

        Public Overrides Function GetIconBitmap() As Object
            Return My.Resources.uo_heater_32
        End Function

        Public Overrides Function GetDisplayDescription() As String
            Return ResMan.GetLocalString("HEAT_Desc")
        End Function

        Public Overrides Function GetDisplayName() As String
            Return ResMan.GetLocalString("HEAT_Name")
        End Function

        Public Overrides Sub CloseEditForm()
            If f IsNot Nothing Then
                If Not f.IsDisposed Then
                    f.Close()
                    f = Nothing
                End If
            End If
        End Sub

        Public Overrides ReadOnly Property MobileCompatible As Boolean
            Get
                Return True
            End Get
        End Property
        Public Overrides Function GetReport(su As IUnitsOfMeasure, ci As Globalization.CultureInfo, numberformat As String) As String

            Dim str As New Text.StringBuilder

            Dim istr, ostr As MaterialStream
            istr = Me.GetInletMaterialStream(0)
            ostr = Me.GetOutletMaterialStream(0)

            istr.PropertyPackage.CurrentMaterialStream = istr

            str.AppendLine("Heater: " & Me.GraphicObject.Tag)
            str.AppendLine("Property Package: " & Me.PropertyPackage.ComponentName)
            str.AppendLine()
            str.AppendLine("Inlet conditions")
            str.AppendLine()
            str.AppendLine("    Temperature: " & SystemsOfUnits.Converter.ConvertFromSI(su.temperature, istr.Phases(0).Properties.temperature.GetValueOrDefault).ToString(numberformat, ci) & " " & su.temperature)
            str.AppendLine("    Pressure: " & SystemsOfUnits.Converter.ConvertFromSI(su.pressure, istr.Phases(0).Properties.pressure.GetValueOrDefault).ToString(numberformat, ci) & " " & su.pressure)
            str.AppendLine("    Mass flow: " & SystemsOfUnits.Converter.ConvertFromSI(su.massflow, istr.Phases(0).Properties.massflow.GetValueOrDefault).ToString(numberformat, ci) & " " & su.massflow)
            str.AppendLine("    Volumetric flow: " & SystemsOfUnits.Converter.ConvertFromSI(su.volumetricFlow, istr.Phases(0).Properties.volumetric_flow.GetValueOrDefault).ToString(numberformat, ci) & " " & su.volumetricFlow)
            str.AppendLine("    Vapor fraction: " & istr.Phases(2).Properties.molarfraction.GetValueOrDefault.ToString(numberformat, ci))
            str.AppendLine("    Compounds: " & istr.PropertyPackage.RET_VNAMES.ToArrayString)
            str.AppendLine("    Molar composition: " & istr.PropertyPackage.RET_VMOL(PropertyPackages.Phase.Mixture).ToArrayString(ci))
            str.AppendLine()
            str.AppendLine("Calculation parameters")
            str.AppendLine()
            str.AppendLine("    Calculation mode: " & CalcMode.ToString)
            Select Case Me.CalcMode
                Case CalculationMode.HeatAdded
                    str.AppendLine("    Heat added: " & SystemsOfUnits.Converter.ConvertFromSI(su.heatflow, Me.DeltaQ).ToString(numberformat, ci) & " " & su.heatflow)
                Case CalculationMode.OutletTemperature
                    str.AppendLine("    Outlet temperature: " & SystemsOfUnits.Converter.ConvertFromSI(su.temperature, Me.OutletTemperature).ToString(numberformat, ci) & " " & su.temperature)
                Case CalculationMode.OutletVaporFraction
                    str.AppendLine("    Outlet vapor mole fraction: " & Me.OutletVaporFraction.GetValueOrDefault.ToString(numberformat, ci))
            End Select
            str.AppendLine("    Efficiency: " & Me.Eficiencia.GetValueOrDefault.ToString(numberformat, ci))
            str.AppendLine("    Pressure drop: " & SystemsOfUnits.Converter.ConvertFromSI(su.pressure, Me.DeltaP.GetValueOrDefault).ToString(numberformat, ci) & " " & su.deltaP)
            str.AppendLine()
            str.AppendLine("Results")
            str.AppendLine()
            Select Case Me.CalcMode
                Case CalculationMode.HeatAdded
                    str.AppendLine("    Outlet temperature: " & SystemsOfUnits.Converter.ConvertFromSI(su.temperature, Me.OutletTemperature).ToString(numberformat, ci) & " " & su.temperature)
                    str.AppendLine("    Outlet vapor mole fraction: " & Me.OutletVaporFraction.GetValueOrDefault.ToString(numberformat, ci))
                Case CalculationMode.OutletTemperature
                    str.AppendLine("    Outlet vapor mole fraction: " & Me.OutletVaporFraction.GetValueOrDefault.ToString(numberformat, ci))
                    str.AppendLine("    Heat added: " & SystemsOfUnits.Converter.ConvertFromSI(su.heatflow, Me.DeltaQ.GetValueOrDefault).ToString(numberformat, ci) & " " & su.heatflow)
                Case CalculationMode.OutletVaporFraction
                    str.AppendLine("    Outlet temperature: " & SystemsOfUnits.Converter.ConvertFromSI(su.temperature, Me.OutletTemperature).ToString(numberformat, ci) & " " & su.temperature)
                    str.AppendLine("    Heat added: " & SystemsOfUnits.Converter.ConvertFromSI(su.heatflow, Me.DeltaQ.GetValueOrDefault).ToString(numberformat, ci) & " " & su.heatflow)
            End Select

            Return str.ToString

        End Function

        Public Overrides Function GetPropertyDescription(p As String) As String
            If p.Equals("Calculation Mode") Then
                Return "Select the calculation mode of the Heater/Cooler. This will define which variables you need to specify."
            ElseIf p.Equals("Pressure Drop") Then
                Return "Enter the desired pressure drop of the heater/cooler."
            ElseIf p.Equals("Outlet Temperature") Then
                Return "If you chose 'Outlet Temperature' as the calculation mode, enter the desired value. If you chose a different calculation mode, this parameter will be calculated."
            ElseIf p.Equals("Heat Added") Then
                Return "If you chose 'Heat Added' as the calculation mode, enter the desired value. If you chose a different calculation mode, this parameter will be calculated."
            ElseIf p.Equals("Efficiency (%)") Then
                Return "Enter the desired efficiency of the heating/cooling process. This defines how much energy flow is actually added or removed to/from the inlet stream."
            ElseIf p.Equals("Outlet Vapor Fraction") Then
                Return "If you chose 'Outlet Vapor Fraction' as the calculation mode, enter the desired value. If you chose a different calculation mode, this parameter will be calculated."
            Else
                Return p
            End If
        End Function

    End Class

End Namespace
