# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_005_Matriz_Comercial_B1_Factoring

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC **************************************************************************
# MAGIC * Nombre: 
# MAGIC * Ruta: 
# MAGIC * Autor: Esteban Gándara
# MAGIC * Ing. Rafael Montecinos - rafael.montecinost@bci.cl
# MAGIC * Fecha: 19/05/2025
# MAGIC * Descripción: 
# MAGIC * Documentación: 
# MAGIC ***************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Mantenciones
# MAGIC **************************************************************************
# MAGIC #### Mantención Nro: 
# MAGIC * Autor: <Nombre Autor> (<Empresa del Autor (Bci/Otra)>) - Ing. SW BCI: <Nombre Ing. SW BCI>
# MAGIC * Fecha: <dd/mm/yyyy> 
# MAGIC * Descripción: <Descripción de la mantención>      
# MAGIC ************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tablas Entrada y Salida
# MAGIC **************************************************************************
# MAGIC #### Tablas Entrada: 
# MAGIC * 
# MAGIC * 
# MAGIC ***************************************************************************
# MAGIC #### Tablas Salida: 
# MAGIC * 

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variables

# COMMAND ----------

dbutils.widgets.removeAll()
dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso :")
dbutils.widgets.text("platinum_temp_dbW","","02 platinum temp db:")
dbutils.widgets.text("db_location_platinum_tempW","","03 Location DB Platinum Temp:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

platinum_temp_dbX = dbutils.widgets.get("platinum_temp_dbW")
spark.conf.set("bci.platinum_temp_dbX", platinum_temp_dbX)

db_location_platinum_tempX = dbutils.widgets.get("db_location_platinum_tempW")
spark.conf.set("bci.db_location_platinum_tempX", db_location_platinum_tempX)

print("*****Parámetros*****")
print("fechaProcesoX: " + fechaProcesoX)
print("platinum_temp_dbX: " + platinum_temp_dbX)
print("db_location_platinum_tempX: " + db_location_platinum_tempX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Librerías

# COMMAND ----------

import json
from datetime import datetime, date, time, timedelta
from dateutil.relativedelta import relativedelta

# Obtener fecha actual
current_date_str = datetime.now().strftime("%Y-%m-%d")
current_date = datetime.strptime(current_date_str, "%Y-%m-%d")
formatted_date = str(current_date.strftime("%Y%m%d"))

# COMMAND ----------

# MAGIC %md
# MAGIC ###Asigan Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)
anomesdia2 = str(ano+'/'+mes+'/'+dia)
anomesdia3 = str(ano+'-'+mes+'-'+'01')


print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)
print("fecha_formato8: " + anomesdia2)
print("fecha_formato9: " + anomesdia3)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Funciones

# COMMAND ----------

# MAGIC %run "./Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ## Validaciones

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
valida_param_vacio(platinum_temp_dbX,'platinum_temp_dbX')
valida_param_vacio(db_location_platinum_tempX,'db_location_platinum_tempX')

# COMMAND ----------

valida_bd(platinum_temp_dbX, 'platinum_temp_dbX')

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Fecha vacía

# COMMAND ----------

fecha_vacia(fechaProcesoX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Fecha futura

# COMMAND ----------

fecha_futura(fechaProcesoX)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio de Lógica

# COMMAND ----------

# MAGIC %md
# MAGIC ### Creación de tablas temporales que se eliminarán al final de la ejecución
# MAGIC
# MAGIC **************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea la tabla final con los resultados de provisiones del grupo comercial de Factoring, formateando los datos para su salida definitiva.
# MAGIC
# MAGIC **Tabla de salida**: `dsr_plt_bcitemp_db.Tmp_Prov_Gr_Com_Fact_B1_Result_0719`
# MAGIC
# MAGIC **variables de salida:**
# MAGIC * **ope_num**: Número de la operación.
# MAGIC * **rut**: RUT del cliente.
# MAGIC * **dv**: DV del RUT del cliente.
# MAGIC * **rut_deudor_fact**: RUT del deudor de factoring.
# MAGIC * **dv_deudor_fact**: DV del RUT del deudor de factoring.
# MAGIC * **doc_num_documento**: Número del documento de factoring.
# MAGIC * **Deuda_TotalIFRS**: Saldo total de la deuda IFRS.
# MAGIC * **seg_n2**: Segmento N2 de clasificación.
# MAGIC * **Clasificacion_Deudor_fact**: Clasificación crediticia del deudor de factoring.
# MAGIC * **Dias_Mora**: Número de días de mora.
# MAGIC * **Tramo_Mora**: Rango de días de mora (ej. "1-29", "30-59").
# MAGIC * **doc_num_operacion_fact**: Número de la operación de factoring.
# MAGIC * **doc_id_documento_fact**: ID del documento de factoring.
# MAGIC * **Responsabilidad_CedenteFactoring**: Código de responsabilidad del cedente.
# MAGIC * **Tipo_Prestamo**: Descripción del tipo de préstamo.
# MAGIC * **Ind_Castigo**: Indicador de castigo (1 si está castigado).
# MAGIC * **Colo_EsCardetIrr**: Indicador de cartera deteriorada o irregular.
# MAGIC * **PI**: Probabilidad de Incumplimiento.
# MAGIC * **PDI**: Pérdida Dado el Incumplimiento.
# MAGIC * **PI_A**: PI ajustada por aval.
# MAGIC * **PDI_A**: PDI ajustada por aval.
# MAGIC * **PROV_d00**: Provisión calculada sin mitigación.
# MAGIC * **Banco**: Nombre del banco (siempre "BCI").
# MAGIC * **Prestamo**: Tipo de préstamo.
# MAGIC * **Matriz**: Código de la matriz de riesgo (ej. "COM", "EST").
# MAGIC * **PE**: Pérdida Esperada (PI * PDI).
# MAGIC * **PE_A**: Pérdida Esperada ajustada por aval (PI_A * PDI_A).
# MAGIC * **FACT_A**: Factor de ajuste por aval.
# MAGIC * **periodo**: Período del proceso en formato AAAA-MM-DD.
# MAGIC * **Negocio_Id**: ID del tipo de negocio.
# MAGIC * **tipo**: Tipo de mitigador aplicado (ej. "AVL").
# MAGIC * **Monto_Avalado**: Monto de la deuda cubierto por el aval.
# MAGIC * **PROV_cm**: Provisión final con mitigación (si aplica).

# COMMAND ----------

# MAGIC %md
# MAGIC #### Prov_Gr_Com_Fact_B1_Result_0719

# COMMAND ----------

drop_tmp_008_Tmp_Prov_Gr_Com_Fact_B1_Result_0719 = """ DROP TABLE IF EXISTS """ + platinum_temp_dbX + """.Tmp_Prov_Gr_Com_Fact_B1_Result_0719 """

# COMMAND ----------

sql_safe(drop_tmp_008_Tmp_Prov_Gr_Com_Fact_B1_Result_0719)

# COMMAND ----------

dbutils.fs.rm(db_location_platinum_tempX+"Tmp_Prov_Gr_Com_Fact_B1_Result_0719", True)

# COMMAND ----------

create_tmp_008_Tmp_Prov_Gr_Com_Fact_B1_Result_0719 = """ CREATE TABLE IF NOT EXISTS """ + platinum_temp_dbX + """.Tmp_Prov_Gr_Com_Fact_B1_Result_0719 
USING DELTA
PARTITIONED BY (periodo)
LOCATION '""" + db_location_platinum_tempX + """Tmp_Prov_Gr_Com_Fact_B1_Result_0719'
AS (
select 
Cod_niid AS ope_num
--,dpf_fec_proceso
,cli_idc as rut
,cli_vrt as dv
--,cli_rzn_soc
,ddr_idc as rut_deudor_fact
,ddr_vrt as dv_deudor_fact
--,ddr_rzn_soc
--,pdt_des_cra
,doc_num_documento
--,doc_num_cuota
,Deuda_TotalIFRS
--,dod_cod_cobranza
--,cod_aec
--,dpf_ind_responsabilidad
--,dpf_fec_vencimiento
,Clasificacion as seg_n2
--,dod_ind_cartera
--,Clasificacion_Cliente
,Clasificacion_Deudor as Clasificacion_Deudor_fact
,Dias_Mora
,CASE WHEN Dias_Mora=0 and Colo_EsCardetIrr=0 then "0"
WHEN Dias_Mora>=1 and Dias_Mora<=29 and Colo_EsCardetIrr=0 then "1-29"
WHEN Dias_Mora>=30 and Dias_Mora<=59 and Colo_EsCardetIrr=0 then "30-59"
WHEN Dias_Mora>=60 and Dias_Mora<=89 and Colo_EsCardetIrr=0 then "60-89"
else "Incumpl" END AS Tramo_Mora
,doc_num_operacion as doc_num_operacion_fact
,doc_id_documento as doc_id_documento_fact
--,doc_ind_fin_mes
--,dpf_pct_pi
--,dpf_pct_pdi
--,dpf_mto_pe
--,cli_rut
,Responsabilidad_CedenteFactoring
,Tipo_Prestamo
--,Periodo_Id
,Ind_Castigo
,Colo_EsCardetIrr
--,Calif_Deudor_Permitida
,PI
,PDI
,PI_A
,PDI_A
--,Mitigador
,PI*PDI*Deuda_TotalIFRS AS PROV_d00
,"BCI" AS Banco
,Tipo_Prestamo AS Prestamo
,CASE WHEN trim(Tipo_Prestamo)="ESTUDIANTIL" then "EST"
WHEN trim(Tipo_Prestamo)="LEASING" then "LEA"
else "COM" END AS Matriz
,PI * PDI AS PE
,PI_A * PDI_A AS PE_A
,1 AS FACT_A
,'"""+anomesdia3+"""' AS periodo
,CASE WHEN trim(pdt_des_cra)="IC" or trim(pdt_des_cra)="IF" or trim(pdt_des_cra)="EF" or trim(pdt_des_cra)="BF" then 24 
ELSE 23 END AS Negocio_Id
,CASE WHEN Calif_Deudor_Permitida=1 then "AVL" ELSE "" END AS tipo
,CASE WHEN Calif_Deudor_Permitida=1 then Deuda_TotalIFRS ELSE 0 END AS Monto_Avalado
,PROV_d00 as PROV_cm
from """+platinum_temp_dbX+""".Tmp_Fundir_Prov_d00
)
"""

# COMMAND ----------

sql_safe(create_tmp_008_Tmp_Prov_Gr_Com_Fact_B1_Result_0719)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")