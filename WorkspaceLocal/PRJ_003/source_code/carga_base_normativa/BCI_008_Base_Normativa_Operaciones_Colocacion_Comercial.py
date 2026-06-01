# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_008_Base_Normativa_Operaciones_Colocacion_Comercial

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC **************************************************************************
# MAGIC * Nombre: BCI_008_Base_Normativa_Operacion_Colocacion_Comercial.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor:  BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de insertar información de Operaciones Colocaciones Comerciales (Grupales e Individuales) Tipos de Activo 11
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
# MAGIC #### Tabla Entrada ADLS: 
# MAGIC * tmp_d00_deuda_act_ctg
# MAGIC * tmp_modelo_com_gr
# MAGIC ***************************************************************************
# MAGIC #### Tabla Salida Temporal: 
# MAGIC * base_archivos_normativos (Activo 11 Tabla 89 Manual CMF)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variables

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Captura de Variables

# COMMAND ----------

dbutils.widgets.removeAll()
dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso :")
dbutils.widgets.text("db_platinumW","","02 platinum DB:")
dbutils.widgets.text("db_plat_tempW","","03 platinum temp db:")
dbutils.widgets.text("db_location_plat_tempW","","04 Location platinum temp db:")
dbutils.widgets.text("db_RiesgoCredW","","05 slv_RiesgoCred_RiesgoCredPer DB:")
dbutils.widgets.text("db_slv_ParametricasW","","06 Parametricas DB:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_platinumX = dbutils.widgets.get("db_platinumW")
spark.conf.set("bci.db_platinumX", db_platinumX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)

db_RiesgoCredX = dbutils.widgets.get("db_RiesgoCredW")
spark.conf.set("bci.db_RiesgoCredX", db_RiesgoCredX)

db_slv_ParametricasX = dbutils.widgets.get("db_slv_ParametricasW")
spark.conf.set("bci.db_slv_ParametricasX", db_slv_ParametricasX)

print("fechaProcesoX: " + fechaProcesoX)
print("db_platinumX: " + db_platinumX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_location_plat_tempX: " + db_location_plat_tempX)
print("db_RiesgoCredX: " + db_RiesgoCredX)
print("db_slv_ParametricasX: " + db_slv_ParametricasX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Carga de funciones

# COMMAND ----------

# MAGIC %run "../../Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ## Validación de ingreso de parámetros

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Parametro Vacio

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
valida_param_vacio(db_platinumX,'db_platinumX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_RiesgoCredX,'db_RiesgoCredX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')
valida_param_vacio(db_slv_ParametricasX,'db_slv_ParametricasX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Base de Datos

# COMMAND ----------

valida_bd(db_platinumX, 'db_platinumX')
valida_bd(db_slv_ParametricasX, 'db_slv_ParametricasX')
valida_bd(db_plat_tempX, 'db_plat_tempX')
valida_bd(db_RiesgoCredX, 'db_RiesgoCredX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Fecha Valida

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Fecha Futura

# COMMAND ----------

valida_fecha_futura(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Ruta 

# COMMAND ----------

valida_ruta(db_location_plat_tempX, 'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ## Asigan Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)

print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ## Creacion tablas temporales

# COMMAND ----------

# MAGIC %md
# MAGIC ###Tabla tmp_col_com_repetidas_mod_ind

# COMMAND ----------

paso_tb_del_tmp_colcomer_rep  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_col_com_repetidas_mod_ind"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_colcomer_rep)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_col_com_repetidas_mod_ind", True)

# COMMAND ----------

paso_tb_crea_tmp_colcomer_rep  = """CREATE TABLE """ + db_plat_tempX + """.tmp_col_com_repetidas_mod_ind
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente INT COMMENT 'RUT del Cliente Titular',
  dv_cliente STRING COMMENT 'Digito verificador del cliente titular',
	cod_ope_original STRING COMMENT 'Operación Original (TRZ)',
  cod_num_operacion STRING COMMENT 'Número Operación',
  cod_tip_cart STRING COMMENT 'Tipo cartera (OPE_VIGENTE) ',
	cod_tipo_ope STRING COMMENT 'Tipo-subtipo del D00',
	des_producto_ope STRING COMMENT  'Descripción Tipo de Cartera',
  des_banca_ope STRING COMMENT 'Descripción operacion banca',
	cod_cartera_ope STRING COMMENT 'Subtipo cartera',
	mto_deuda_ope	   DECIMAL(18,0) COMMENT 'Saldo total ifrs',
  num_dias_mora_ope INT COMMENT 'Dias de mora',
  ind_cdet STRING COMMENT 'Indicador cartera deterioro',
  fec_ingreso_deteriodo_ope DATE COMMENT 'Fecha ingreso de deterioro de la operación',
  cod_cctb STRING COMMENT 'Cuenta contable',
  fl_ope_reneg STRING COMMENT 'Flag de cartera renegociada'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_col_com_repetidas_mod_ind' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_colcomer_rep)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Desarrollo Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 1: Eliminación Operaciones Comerciales (Grupal e Individual) para fecha de Proceso

# COMMAND ----------

query_delete_gr = """DELETE FROM """ + db_platinumX + """.base_archivos_normativos WHERE periodo_id = """+anomes+""" and cod_tabla_89 = 11  """

# COMMAND ----------

sql_safe(query_delete_gr)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 2: Insert Operaciones de Colocaciones Comerciales Activas

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones Comerciales Activas (Tipo Activo = 11)
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft 
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera (OPE_VIGENTE)
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente GR/IND
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: pct_fcc
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: MONTO_GARANTIA' ,
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado
# MAGIC - **fec_proceso**: Fecha de cierre

# COMMAND ----------

ult_dia_mes = ultimo_dia_habil(db_slv_ParametricasX,anomes)

ult_dia = ult_dia_mes.toPandas()
fec_proceso  = ult_dia.iloc[0]["periodo_id"]

ano_proceso = str(fec_proceso)[:4]
mes_proceso = str(fec_proceso)[4:][:2]
dia_proceso = str(fec_proceso)[6:][:2]
date_proceso = str(ano_proceso+'-'+mes_proceso+'-'+dia_proceso)

print(date_proceso)

# COMMAND ----------

query_ins_col_com_act = """ insert into """ + db_platinumX + """.base_archivos_normativos
select a.periodo_id
	,a.rut_cliente
  ,a.dv_cliente
	,a.cod_ope_original
  ,a.cod_num_operacion
  ,a.cod_tip_cart 
	,a.cod_tipo_ope
	,a.des_producto_ope
  ,a.des_banca_ope
	,a.cod_cartera_ope
	,a.mto_deuda_ope as mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb	
  ,a.fl_ope_reneg 
  ,-998                              as cod_tabla_34
	,11 as cod_tipo_activo
	,'COLOCACION COMERCIAL' as nombre_activo 
	,b.pct_pi
  ,b.pct_pdi
  ,b.pct_pe
	,b.mnt_prov_oficial as mnt_provision
	,'GR' as cod_tipo_cli
	,0 as mto_exposicion
	,0 as factor_expo
	,b.des_tipo_gtia as TIPO_GARANTIA				
	,b.mnt_garantia as MONTO_GARANTIA
	,b.pct_ltv as PTVG 
	,b.pct_pi_metodo_interno as pct_pi_metodo_interno
  ,b.pct_pdi_metodo_interno as  pct_pdi_metodo_interno
  ,b.pct_pe_metodo_interno  as pct_pe_metodo_interno
	,b.mnt_prov_int as mto_PROV_METODO_INTERNO 
	,ROUND(b.mnt_avalado,0) as mto_avalado
 	,'"""+str(date_proceso)+"""'
from """+db_plat_tempX+""".tmp_colcomer a
inner join """+db_plat_tempX+""".tmp_modelo_com_gr b
  on a.periodo_id = b.periodo_id
  and trim(a.cod_cartera_ope) = trim(b.cod_stp_cart)
  and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
where a.periodo_id =  """+anomes+"""
	and trim(a.cod_cartera_ope) = 'ACT' """

# COMMAND ----------

sql_safe(query_ins_col_com_act)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 3: Insert Operaciones Individuales Excluyendo Repetidos

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones Individuales, excluyendo operaciones repetidas (Tipo Activo = 11), estas operaciones se generan en Notebook 02
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft 
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera (OPE_VIGENTE)
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Calificacion Modelo Individual
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: pct_fcc
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: MONTO_GARANTIA' ,
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado
# MAGIC - **fec_proceso**: Fecha de cierre

# COMMAND ----------

query_ins_col_individual = """ insert into """ + db_platinumX + """.base_archivos_normativos
SELECT a.periodo_id
  ,a.rut_cliente
  ,a.dv_cliente
	,a.cod_ope_original
  ,a.cod_num_operacion
  ,a.cod_tip_cart 
	,a.cod_tipo_ope
	,a.des_producto_ope
  ,a.des_banca_ope
	,a.cod_cartera_ope
	,a.mto_deuda_ope as mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb	
  ,a.fl_ope_reneg 
  ,-998                              as cod_tabla_34
  ,11 as cod_tipo_activo
	,'COLOCACION COMERCIAL' as nombre_activo 
  ,b.pct_pi
  ,b.pct_pdi
  ,b.pct_pe
	,b.mnt_provision	
	,b.cod_calificacion
	,0 as mnt_exposicion	
	,0 as pct_factor_exp
	,b.cod_tipo_garantia
  ,b.mnt_garantia
	,0 as pct_ltv
  ,0 as pct_pi_metodo_interno
  ,0 as pct_pdi_metodo_interno
  ,0 as pct_pe_metodo_interno
  ,0 as mnt_provision_interno
  ,0 as mnt_avalado
  ,'"""+str(date_proceso)+"""'
FROM """+db_plat_tempX+""".tmp_colcomer a
	INNER JOIN """+db_plat_tempX+""".tmp_modelo_indiv b
			ON a.periodo_id = b.periodo_id
		  AND ((trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
            AND trim(b.cod_tipo_deu) = 'NCONT'
            AND b.mnt_provision <> 0) 
        OR (trim(a.cod_num_operacion) = trim(b.cod_num_operacion)			
            AND trim(b.cod_tipo_deu) = 'NCONT'
            AND b.mnt_provision = 0 
            AND (substring(trim(a.cod_num_operacion),1,1) in ('C','V'))) 
        OR (trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
				    AND trim(b.cod_tipo_deu) = 'NCONT'
				    AND b.mnt_provision = 0 
            AND substring(trim(a.cod_tipo_ope),1,3) = 'TDC'))
WHERE a.periodo_id = """+anomes+"""
  AND trim(a.cod_num_operacion) NOT IN (SELECT trim(cod_num_operacion) cod_num_operacion FROM """+db_plat_tempX+""".tmp_individual_repetidos)
  """

# COMMAND ----------

sql_safe(query_ins_col_individual)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 4: Obtencion Operaciones de Comercial con Operaciones Individuales Repetidas

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de colocaciones comerciales repetidas provenientes desde tabla temporal tmp_individual_repetidos generada en Notebook 002.
# MAGIC  
# MAGIC **Tabla de salida**: tmp_col_com_repetidas_mod_ind(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: periodo proceso formato SSAAMM
# MAGIC - **rut_cliente**: Parte numerica de rut cliente
# MAGIC - **dv_cliente** : Dígito verificador de rut cliente
# MAGIC - **cod_ope_original** : Código Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Código Número Operación
# MAGIC - **cod_tip_cart**: Código Tipo cartera 
# MAGIC - **cod_tipo_ope**: Código Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Código Subtipo cartera
# MAGIC - **mto_deuda_ope**:	Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada

# COMMAND ----------

query_tmp_colcomer_rep = """INSERT INTO """ + db_plat_tempX + """.tmp_col_com_repetidas_mod_ind
SELECT a.periodo_id
  ,a.rut_cliente
  ,a.dv_cliente
	,a.cod_ope_original
  ,a.cod_num_operacion
  ,a.cod_tip_cart 
	,a.cod_tipo_ope
	,a.des_producto_ope
  ,a.des_banca_ope
	,a.cod_cartera_ope
	,a.mto_deuda_ope as mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb	
  ,a.fl_ope_reneg 
FROM """+db_plat_tempX+""".tmp_colcomer a
	INNER JOIN """+db_plat_tempX+""".tmp_individual_repetidos b
    on a.periodo_id = b.periodo_id
    and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
WHERE a.periodo_id = """+anomes+""" 
"""

# COMMAND ----------

sql_safe(query_tmp_colcomer_rep)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 5: Insert Operaciones Individuales Repetidas  

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de Operaciones Individuales Repetidas, agrupando por Max Deuda y la suma de Provisiones (Tipo Activo = 11), estas operaciones se generan en Paso anterior
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft 
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera (OPE_VIGENTE)
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Calificacion Modelo Individual
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: pct_fcc
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: MONTO_GARANTIA' ,
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado
# MAGIC - **fec_proceso**: Fecha de cierre

# COMMAND ----------

query_ins_col_com_2 = """ insert into """ + db_platinumX + """.base_archivos_normativos
SELECT a.periodo_id
  ,a.rut_cliente
  ,a.dv_cliente
	,a.cod_ope_original
  ,a.cod_num_operacion
  ,a.cod_tip_cart 
	,a.cod_tipo_ope
	,a.des_producto_ope
  ,a.des_banca_ope
	,a.cod_cartera_ope
	,a.mto_deuda_ope as mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb	
  ,a.fl_ope_reneg 
  ,-998                     as cod_tabla_34
  ,11                       as cod_tipo_activo
	,'COLOCACION COMERCIAL'   as nombre_activo 
  ,max(b.pct_pi)            as pct_pi
  ,max(b.pct_pdi)           as pct_pdi
  ,max(b.pct_pe)            as pct_pe
	,sum(b.mnt_provision)     as mnt_provision
	,b.cod_calificacion
	,0                        as mnt_exposicion	
	,0                        as pct_factor_exp
	,max(b.cod_tipo_garantia) as cod_tipo_garantia
  ,max(b.mnt_garantia)      as mnt_garantia
	,0 as pct_ltv
  ,0 as pct_pi_metodo_interno
  ,0 as pct_pdi_metodo_interno
  ,0 as pct_pe_metodo_interno
  ,0 as mnt_provision_interno
  ,0 as mnt_avalado
  ,'"""+str(date_proceso)+"""'
FROM """+db_plat_tempX+""".tmp_col_com_repetidas_mod_ind a
	LEFT JOIN """+db_plat_tempX+""".tmp_modelo_indiv b
			ON a.periodo_id = b.periodo_id
      AND (trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
				    AND trim(b.cod_tipo_deu) = 'NCONT'
				    AND b.mnt_provision <> 0) 
          OR (trim(a.cod_num_operacion) = trim(b.cod_num_operacion)		
            AND trim(b.cod_tipo_deu) = 'NCONT'
				    AND b.mnt_provision = 0 
            AND substring(trim(a.cod_num_operacion), 1,1) in ('C', 'V'))	
          OR (trim(a.cod_num_operacion) = trim(b.cod_num_operacion)		
            AND trim(b.cod_tipo_deu) = 'NCONT'
				    AND b.mnt_provision = 0 
            AND substring(trim(a.cod_tipo_ope), 1,3) = 'TDC'
            )	
WHERE a.periodo_id = """+anomes+"""
GROUP BY a.periodo_id
  ,a.rut_cliente
  ,a.dv_cliente
	,a.cod_ope_original
  ,a.cod_num_operacion
  ,a.cod_tip_cart 
	,a.cod_tipo_ope
	,a.des_producto_ope
  ,a.des_banca_ope
	,a.cod_cartera_ope
	,a.mto_deuda_ope 
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb	
  ,a.fl_ope_reneg 
  ,b.cod_calificacion
"""

# COMMAND ----------

sql_safe(query_ins_col_com_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_platinumX + """.base_archivos_normativos  where periodo_id = """+anomes+""" and cod_tabla_89 in (11) """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal base_archivos_normativos ")

# COMMAND ----------

# MAGIC %md
# MAGIC # Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")