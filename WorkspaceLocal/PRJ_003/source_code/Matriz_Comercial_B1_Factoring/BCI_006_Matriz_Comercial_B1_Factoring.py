# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_006_Matriz_Comercial_B1_Factoring

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
dbutils.widgets.text("platinum_dbW","","03 platinum db:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

platinum_temp_dbX = dbutils.widgets.get("platinum_temp_dbW")
spark.conf.set("bci.platinum_temp_dbX", platinum_temp_dbX)

platinum_dbX = dbutils.widgets.get("platinum_dbW")
spark.conf.set("bci.platinum_dbX", platinum_dbX)

print("*****Parámetros*****")
print("fechaProcesoX: " + fechaProcesoX)
print("platinum_temp_dbX: " + platinum_temp_dbX)
print("platinum_dbX: " + platinum_dbX)

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


print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)
print("fecha_formato8: " + anomesdia2)

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
valida_param_vacio(platinum_dbX,'platinum_dbX')

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
# MAGIC #### Prov_Gr_Com_Fact_B1_0719_NUEVO.csv

# COMMAND ----------

delete_prv_fac_ft_b1_com_ = """ DELETE FROM """ + platinum_dbX + """.prv_fac_ft_b1_com 
                                WHERE fec_proceso = to_date('""" + fechanormativo + """', 'yyyy-MM-dd') """

# COMMAND ----------

sql_safe(delete_prv_fac_ft_b1_com_)

# COMMAND ----------

insert_prv_fac_ft_b1_com = """ INSERT INTO """ + platinum_dbX + """.prv_fac_ft_b1_com  
(
select
cli_idc AS rut_cliente
,cli_vrt AS dv_cliente
,cli_rzn_soc AS des_razon_social_cliente
,ddr_idc AS rut_deudor
,ddr_vrt AS dv_deudor
,ddr_rzn_soc AS des_razon_social_deudor
,pdt_des_cra AS ind_tipo_documento
,doc_num_documento AS num_documento
,doc_num_cuota AS num_cuota
,Deuda_TotalIFRS AS mnt_saldo_documento
,dod_cod_cobranza AS ind_codigo_cobranza
,cod_aec AS ind_actividad_economica
,dpf_ind_responsabilidad AS ind_responsabilidad_operacion
/*,to_date(dpf_fec_vencimiento, 'yyyy-MM-dd') AS fec_vcto_doc_con_prorroga*/
,date_format(
    to_date(dpf_fec_vencimiento, 'dd-MM-yyyy'),
    'yyyy-MM-dd'
  )  AS fec_vcto_doc_con_prorroga
,"GRUPAL" AS des_tipo_segmento
,dod_ind_cartera AS des_cartera_del_documento
,Clasificacion_Cliente AS cod_clasif_cliente
,Clasificacion_Deudor AS cod_clasif_deudor
,Dias_Mora AS num_dias_mora
,doc_num_operacion AS num_operacion
,doc_id_documento AS num_id_documento
,doc_ind_fin_mes AS ind_fin_de_mes_doc
,CASE WHEN PI_A > 0 THEN PI_A
ELSE PI END as pct_pi
,CASE WHEN PDI_A > 0 THEN PDI_A
ELSE PDI END as pct_pdi
,ROUND((CASE WHEN PI_A > 0 THEN PI_A ELSE PI END)*(CASE WHEN PDI_A > 0 THEN PDI_A ELSE PDI END)*Deuda_TotalIFRS) AS pct_pe
,to_date('""" + fechanormativo + """', 'yyyy-MM-dd') AS fec_proceso
from """+platinum_temp_dbX+""".Tmp_Fundir_Prov_d00
)
"""

# COMMAND ----------

sql_safe(insert_prv_fac_ft_b1_com)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Prov_Gr_Com_Fact_B1_Result_0719_NUEVO.txt

# COMMAND ----------

delete_prv_fac_ft_b1_com_result = """ DELETE FROM """ + platinum_dbX + """.prv_fac_ft_b1_com_result
                                      WHERE fec_proceso = to_date('""" + fechanormativo + """', 'yyyy-MM-dd') """

# COMMAND ----------

sql_safe(delete_prv_fac_ft_b1_com_result)

# COMMAND ----------

insert_prv_fac_ft_b1_com_result = """ INSERT INTO """ + platinum_dbX + """.prv_fac_ft_b1_com_result
select 
Cod_niid AS num_operacion
,cli_idc AS rut_cliente
,cli_vrt AS dv_cliente
,ddr_idc AS rut_deudor
,ddr_vrt AS dv_deudor
,doc_num_documento AS num_documento_fact
,Deuda_TotalIFRS AS mto_saldo_ifrs
,Clasificacion AS ind_segmentacion
,Clasificacion_Deudor AS cod_clasif_deudor
,Dias_Mora AS num_dias_mora
,CASE WHEN Dias_Mora=0 and Colo_EsCardetIrr=0 then "0"
WHEN Dias_Mora>=1 and Dias_Mora<=29 and Colo_EsCardetIrr=0 then "1-29"
WHEN Dias_Mora>=30 and Dias_Mora<=59 and Colo_EsCardetIrr=0 then "30-59"
WHEN Dias_Mora>=60 and Dias_Mora<=89 and Colo_EsCardetIrr=0 then "60-89"
else "Incumpl" END AS des_tramo_mora
,doc_num_operacion AS num_operacion_fact
,doc_id_documento AS num_id_documento_fact
,Responsabilidad_CedenteFactoring AS cod_responsabilidad_cedente
,Ind_Castigo AS cod_castigo
,Colo_EsCardetIrr AS cod_cart_deteriorada
,PI AS pct_pi
,PDI AS pct_pdi
,PI_A AS pct_pi_aval
,PDI_A AS pct_pdi_aval
,ROUND((PI*PDI*Deuda_TotalIFRS),3) AS mnt_provision
,"BCI" as des_entidad
,Tipo_Prestamo as des_tipo_prestamo
,CASE WHEN trim(Tipo_Prestamo)="ESTUDIANTIL" then "EST"
WHEN trim(Tipo_Prestamo)="LEASING" then "LEA"
else "COM" END AS des_matriz
,ROUND((PI * PDI),3) AS pct_pe
,ROUND((PI_A * PDI_A),3) AS pct_pe_aval
,1 AS pct_factor_cubierto_aval
,'"""+ anomes +"""' as id_periodo
,CASE WHEN trim(pdt_des_cra)="IC" or trim(pdt_des_cra)="IF" or trim(pdt_des_cra)="EF" or trim(pdt_des_cra)="BF" then 24 
else 23 END AS ind_negocio
,CASE WHEN Calif_Deudor_Permitida=1 then "AVL" else "" END AS des_tipo_mitigador
,CASE WHEN Calif_Deudor_Permitida=1 then Deuda_TotalIFRS else 0 END AS mnt_avalado
,ROUND(PROV_d00,3) AS mnt_provision_con_mitigacion
,to_date('""" + fechanormativo + """', 'yyyy-MM-dd') AS fec_proceso
FROM """+platinum_temp_dbX+""".Tmp_Fundir_Prov_d00
"""

# COMMAND ----------

sql_safe(insert_prv_fac_ft_b1_com_result)

# COMMAND ----------

# MAGIC %md
# MAGIC
# MAGIC rectifica problemas de datos

# COMMAND ----------

queryupd1 = """ update  """ + platinum_dbX + """.prv_fac_ft_b1_com_result
set num_operacion = replace(num_operacion, '?', '')
where trim(num_operacion) like '%?%' """
sql_safe(queryupd1)


# COMMAND ----------

queryupd2 = """ update  """ + platinum_dbX + """.prv_fac_ft_b1_com_result
set dv_deudor = ''
where trim(dv_deudor) = '?' """
sql_safe(queryupd2)

# COMMAND ----------

queryupd3 = """ update  """ + platinum_dbX + """.prv_fac_ft_b1_com_result
set cod_clasif_deudor = ''
where trim(cod_clasif_deudor) = '?' """
sql_safe(queryupd3)

# COMMAND ----------

# MAGIC %md
# MAGIC
# MAGIC rectifica problemas de datos

# COMMAND ----------

queryupd1 = """ update  """ + platinum_dbX + """.prv_fac_ft_b1_com
set cod_clasif_cliente = ''
where trim(cod_clasif_cliente) = '?' """
sql_safe(queryupd1)

# COMMAND ----------

queryupd2 = """ update  """ + platinum_dbX + """.prv_fac_ft_b1_com
set cod_clasif_deudor = ''
where trim(cod_clasif_deudor) = '?' """
sql_safe(queryupd2)

# COMMAND ----------


queryupd3 = """ update  """ + platinum_dbX + """.prv_fac_ft_b1_com
set dv_deudor = ''
where trim(dv_deudor) = '?' """
sql_safe(queryupd3)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")