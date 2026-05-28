# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_016_Base_Normativa_Operaciones_Linea_Cred_Libre_Disposicion

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC **************************************************************************
# MAGIC * Nombre: BCI_016_Base_Normativa_Operaciones_Linea_Cred_Libre_Disposicion.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor:  BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de insertar información de Operaciones Líneas de crédito de libre disposición Tipos de Activo 51, 52, 53, 54 y otros Otros compromisos de crédito Tipo de Activo 62
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
# MAGIC * tmp_modelo_hip
# MAGIC * tmp_modelo_hip_estandar
# MAGIC * tmp_bci15_modelo_ind_agrp
# MAGIC * slv_RiesgoCred_RiesgoCredPer_db.tbl_prov_gr_con_bci_ope_0722
# MAGIC ***************************************************************************
# MAGIC #### Tabla Salida Temporal: 
# MAGIC * base_archivos_normativos (Activo 51, 52, 53, 54, 62 Tabla 89 Manual CMF)

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
# MAGIC ### Tabla tmp_ope_lc_libre_disposicion

# COMMAND ----------

paso_tb_del_tmp_ope_lc_libre_disp  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_ope_lc_libre_disposicion """

# COMMAND ----------

sql_safe(paso_tb_del_tmp_ope_lc_libre_disp)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_ope_lc_libre_disposicion", True)

# COMMAND ----------

paso_tb_crea_tmp_ope_lc_libre_disp  = """CREATE TABLE """ + db_plat_tempX + """.tmp_ope_lc_libre_disposicion
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
	mnt_deuda_ope	   DECIMAL(32,0) COMMENT 'Saldo total ifrs',
  num_dias_mora_ope INT COMMENT 'Dias de mora',
  ind_cdet STRING COMMENT 'Indicador cartera deterioro',
  fec_ingreso_deteriodo_ope DATE COMMENT 'Fecha ingreso de deterioro de la operación',
  cod_cctb STRING COMMENT 'Cuenta contable',
  cod_cta_ifrs STRING COMMENT 'Cuenta IFRS',
  fl_ope_reneg STRING COMMENT 'Flag de cartera renegociada',
  cod_tipo_activo INT COMMENT 'Tipo Activo Cartera',
	nombre_activo STRING COMMENT 'Nombre Activo Cartera',
	pct_pi DECIMAL(12,6) COMMENT 'probabilidad de incumplimiento del cliente',
  pct_pdi DECIMAL(12,6) COMMENT 'perdida dado el incumplimiento de la operación',
  pct_pe DECIMAL(12,6) COMMENT 'pérdida esperada',
	mnt_provision DECIMAL(32,0) COMMENT 'Prov Oficial',
	cod_tipo_cli STRING COMMENT 'Tipo Cliente GR/IND',
	mnt_exposicion DECIMAL(32,0) COMMENT 'Prov Oficial',
	factor_expo DECIMAL(12,6) COMMENT 'PI',
	des_tipo_gtia STRING COMMENT 'Tipo garantia',		
	mnt_garantia DECIMAL(32,0) COMMENT 'MONTO_GARANTIA' ,
	pct_ltv DECIMAL(12,6) COMMENT 'relación entre la deuda actual y el valor del bien al origen',
	pct_pi_metodo_interno DECIMAL(12,6) COMMENT 'PI metodo interno',
  pct_pdi_metodo_interno DECIMAL(12,6) COMMENT 'PDI metodo interno',
  pct_pe_metodo_interno DECIMAL(12,6) COMMENT 'PE metodo interno',
	mnt_prov_metodo_interno  DECIMAL(32,0) COMMENT 'Provisión de la Colocación',
	mnt_avalado DECIMAL(32,0) COMMENT 'MONTO_AVALADO'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_ope_lc_libre_disposicion' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_ope_lc_libre_disp)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla tmp_ope_lc_libre_disposicion_2

# COMMAND ----------

paso_tb_del_tmp_ope_lc_libre_disp_2  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_ope_lc_libre_disposicion_2 """

# COMMAND ----------

sql_safe(paso_tb_del_tmp_ope_lc_libre_disp_2)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_ope_lc_libre_disposicion_2", True)

# COMMAND ----------

paso_tb_crea_tmp_ope_lc_libre_disp_2  = """CREATE TABLE """ + db_plat_tempX + """.tmp_ope_lc_libre_disposicion_2
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
	mnt_deuda_ope	   DECIMAL(32,0) COMMENT 'Saldo total ifrs',
  num_dias_mora_ope INT COMMENT 'Dias de mora',
  ind_cdet STRING COMMENT 'Indicador cartera deterioro',
  fec_ingreso_deteriodo_ope DATE COMMENT 'Fecha ingreso de deterioro de la operación',
  cod_cctb STRING COMMENT 'Cuenta contable',
  cod_cta_ifrs STRING COMMENT 'Cuenta IFRS',
  fl_ope_reneg STRING COMMENT 'Flag de cartera renegociada',
  cod_tipo_activo INT COMMENT 'Tipo Activo Cartera',
	nombre_activo STRING COMMENT 'Nombre Activo Cartera',
	pct_pi DECIMAL(12,6) COMMENT 'probabilidad de incumplimiento del cliente',
  pct_pdi DECIMAL(12,6) COMMENT 'perdida dado el incumplimiento de la operación',
  pct_pe DECIMAL(12,6) COMMENT 'pérdida esperada',
	mnt_provision DECIMAL(32,0) COMMENT 'Prov Oficial',
	cod_tipo_cli STRING COMMENT 'Tipo Cliente GR/IND',
	mnt_exposicion DECIMAL(32,0) COMMENT 'Prov Oficial',
	factor_expo DECIMAL(12,6) COMMENT 'PI',
	des_tipo_gtia STRING COMMENT 'Tipo garantia',		
	mnt_garantia DECIMAL(32,0) COMMENT 'MONTO_GARANTIA' ,
	pct_ltv DECIMAL(12,6) COMMENT 'relación entre la deuda actual y el valor del bien al origen',
	pct_pi_metodo_interno DECIMAL(12,6) COMMENT 'PI metodo interno',
  pct_pdi_metodo_interno DECIMAL(12,6) COMMENT 'PDI metodo interno',
  pct_pe_metodo_interno DECIMAL(12,6) COMMENT 'PE metodo interno',
	mnt_prov_metodo_interno  DECIMAL(32,0) COMMENT 'Provisión de la Colocación',
	mnt_avalado DECIMAL(32,0) COMMENT 'MONTO_AVALADO'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_ope_lc_libre_disposicion_2' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_ope_lc_libre_disp_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Desarrollo Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 1: Obtencion Operaciones Líneas de crédito de libre disposición comerciales Tipo Activo 51

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones contingentes de Líneas de crédito de libre disposición comerciales utilizando tablas tmp_d00_deuda_act_ctg (Salida de Notebook 1)y tabla tmp_modelo_com_gr (Salida Notebook 003)
# MAGIC
# MAGIC **Tabla de salida**: tmp_ope_lc_libre_disposicion(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente (Calificacion cliente)
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**:relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_linea_credito_com = """ insert into """+db_plat_tempX+""".tmp_ope_lc_libre_disposicion  
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
	,a.mto_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
	,'' as cod_cta_ifrs
  ,a.fl_ope_reneg 
	,51 as cod_tipo_activo
	,'LINEAS COMERCIALES' as nombre_activo 
	,b.pct_pi
	,b.pct_pdi
	,b.pct_pe
	,b.mnt_prov_oficial as mnt_provision
	,case when b.periodo_id is null then 'SIN' else 'GR' end as cod_tipo_cli
	,0 as mto_exposicion
	,0.1 as factor_expo
	,b.des_tipo_gtia as des_tipo_garantia				
	,b.mnt_garantia as mnt_garantia
	,b.pct_ltv as PTVG 
	,b.pct_pi_metodo_interno	
	,b.pct_pdi_metodo_interno	
	,b.pct_pe_metodo_interno	
	,b.mnt_prov_int as mnt_prov_metodo_interno 
	,b.mnt_avalado as mnt_avalado
from """+db_plat_tempX+""".tmp_d00_deuda_act_ctg a
	left join """+db_plat_tempX+""".tmp_modelo_com_gr b
		on a.periodo_id = b.periodo_id
  	and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
    and trim(a.cod_cartera_ope) = trim(b.cod_stp_cart)
where a.periodo_id =  """+anomes+"""
	and trim(a.cod_cartera_ope) = 'CTG'
 	and trim(a.cod_cctb) IN ( '931000068','931000138','931000151','931000024',
										  '931000033','931000036','931000038','931000041',
										  '931000042','931000051','931000054','931000062',
										  '931000066')
  and trim(a.fl_ope_reneg) <> 'C'  
 """

# COMMAND ----------

sql_safe(query_ins_linea_credito_com)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 2: Actualizacion Valores de Líneas de crédito de libre disposición comerciales desde Modelo Individual

# COMMAND ----------

# MAGIC %md
# MAGIC se actualizan operaciones contingentes utilizando tablas tmp_ope_lc_libre_disposicion (Salida paso anterior) y tabla tmp_modelo_indiv (Salida Notebook 002) para valores de modelo interno y estandar
# MAGIC
# MAGIC **Tabla de salida**: tmp_ope_lc_libre_disposicion_2(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente (Calificacion cliente)
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**:relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_linea_credito_com_2 = """ insert into """+db_plat_tempX+""".tmp_ope_lc_libre_disposicion_2
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
  ,a.mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
  ,a.cod_cta_ifrs
  ,a.fl_ope_reneg
  ,a.cod_tipo_activo
  ,a.nombre_activo
  ,coalesce(a.pct_pi, b.pct_pi) pct_pi
  ,coalesce(a.pct_pdi, b.pct_pdi) pct_pdi
  ,coalesce(a.pct_pe, b.pct_pe) pct_pe
  ,coalesce(a.mnt_provision, b.mnt_provision)  as mnt_provision
  ,CASE WHEN trim(a.cod_tipo_cli) = 'SIN' THEN b.cod_calificacion ELSE 'GR' END	as cod_tipo_cli
  ,round((CASE WHEN trim(a.ind_cdet) = 'N' OR (trim(a.ind_cdet) = 'D' AND coalesce(a.pct_pi, b.pct_pi) <> 1) THEN (a.mnt_deuda_ope * 0.1) ELSE 0 END), 0) as mnt_exposicion
  ,a.factor_expo
  ,coalesce(a.des_tipo_gtia, b.cod_tipo_garantia) as des_tipo_gtia
  ,coalesce(a.mnt_garantia, b.mnt_garantia) as mnt_garantia
  ,coalesce(a.pct_ltv, 0)  as pct_ltv
  ,coalesce(a.pct_pi_metodo_interno, 0)   as pct_pi_metodo_interno
  ,coalesce(a.pct_pdi_metodo_interno, 0)  as pct_pdi_metodo_interno
  ,coalesce(a.pct_pe_metodo_interno, 0) as pct_pe_metodo_interno
  ,coalesce(a.mnt_prov_metodo_interno, 0) as mnt_prov_metodo_interno
  ,coalesce(a.mnt_avalado, 0) as mnt_avalado
from """+db_plat_tempX+""".tmp_ope_lc_libre_disposicion  a 
 left join """+db_plat_tempX+""".tmp_modelo_indiv b 
  on a.periodo_id=b.periodo_id
  and trim(a.cod_num_operacion) =trim(b.cod_num_operacion)
  and trim(cod_tipo_deu) = 'CONT'
  --and trim(a.cod_cartera_ope) =trim(b.cod_tipo_activo)
  and b.mnt_provision <> 0
where a.periodo_id = """+anomes+"""  
  and a.cod_tipo_activo = 51
"""

# COMMAND ----------

sql_safe(query_ins_linea_credito_com_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 3: Obtencion Operaciones Líneas de crédito de libre disposición consumo ( tarjetas de crédito - cuenta corriente) Tipo Activo 52 y 53

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones contingentes de Líneas de crédito de libre disposición comerciales utilizando tablas tmp_d00_deuda_act_ctg (Salida de Notebook 1)y tabla tmp_modelo_consumo (Salida Notebook 005) para valores de modelo interno
# MAGIC
# MAGIC **Tabla de salida**: tmp_ope_lc_libre_disposicion(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente (Calificacion cliente)
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**:relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_linea_credito_con = """ insert into """+db_plat_tempX+""".tmp_ope_lc_libre_disposicion  
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
	,a.mto_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
	,'' as cod_cta_ifrs
  ,a.fl_ope_reneg 
	,case when trim(a.cod_cctb) IN ( '931000037','931000040','931000044','931000050', '931000053','931000061','931000067','931000069','931000046') then 52 
        when trim(a.cod_cctb) IN ('931000034','931000035','931000060','931000065', '931000137','931000025','931000031')                   then 53
  else -998 end     as cod_tipo_activo
	,case when trim(a.cod_cctb) IN ( '931000037','931000040','931000044','931000050', '931000053','931000061','931000067','931000069','931000046') then 'TARJETAS CONSUMO'
        when trim(a.cod_cctb) IN ('931000034','931000035','931000060','931000065', '931000137','931000025','931000031')                   then 'CUENTAS CORRIENTES CONSUMO'
  else '' end as nombre_activo 
	,0 as pct_pi
	,0 as pct_pdi
	,0 as pct_pe
	,0 as mnt_provision
	,'GR' as cod_tipo_cli
	,0 as mnt_exposicion
	,0.1 as factor_expo
	,'No_Tiene' as tipo_garantia				
	,0 as mnt_garantia
	,0 as pct_ltv
	,b.pct_pi as pct_pi_metodo_interno	
	,b.pct_pdi as pct_pdi_metodo_interno	
	,b.pct_pe as pct_pe_metodo_interno	
	,b.mnt_provtot_cont  as mnt_PROV_METODO_INTERNO 
	,0 as mnt_avalado
from """+db_plat_tempX+""".tmp_d00_deuda_act_ctg a
	inner join """+db_plat_tempX+""".tmp_modelo_consumo b
		on a.periodo_id = b.periodo_id
  	and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
    and trim(b.cod_modelo) = 'INT'
where a.periodo_id =  """+anomes+"""
 	and trim(a.cod_cartera_ope) = 'CTG'
 	and trim(a.cod_cctb) IN ( '931000037','931000040','931000044','931000050','931000053','931000061','931000067','931000069','931000046', 
  '931000034','931000035','931000060','931000065','931000137','931000025','931000031')
  and trim(a.fl_ope_reneg) <> 'C'  
  """

# COMMAND ----------

sql_safe(query_ins_linea_credito_con)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 4: Actualizacion Valores de Líneas de crédito de libre disposición Consumo (Tarjeta Credito - cuenta corriente) desde Modelo Estandar

# COMMAND ----------

# MAGIC %md
# MAGIC se actualizan operaciones contingentes utilizando tablas tmp_ope_lc_libre_disposicion (Salida paso anterior) y tabla tmp_modelo_consumo (Salida Notebook 005) para valores de modelo estandar
# MAGIC
# MAGIC **Tabla de salida**: tmp_ope_lc_libre_disposicion_2(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente (Calificacion cliente)
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**:relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_linea_credito_cons_2 = """ insert into """+db_plat_tempX+""".tmp_ope_lc_libre_disposicion_2
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
  ,a.mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
  ,a.cod_cta_ifrs
  ,a.fl_ope_reneg
  ,a.cod_tipo_activo
  ,a.nombre_activo
  ,b.pct_pi
  ,b.pct_pdi
  ,b.pct_pe
  ,b.mnt_provtot_cont  as mnt_provision
  ,a.cod_tipo_cli
  ,round((CASE WHEN trim(a.ind_cdet) = 'N' OR (trim(a.ind_cdet) = 'D' AND a.pct_pi_metodo_interno <> 1) THEN (a.mnt_deuda_ope * 0.1) ELSE 0 END), 0) as mnt_exposicion
  ,a.factor_expo
  ,a.des_tipo_gtia
  ,a.mnt_garantia
  ,a.pct_ltv
  ,a.pct_pi_metodo_interno 
  ,a.pct_pdi_metodo_interno 
  ,a.pct_pe_metodo_interno 
  ,a.mnt_prov_metodo_interno 
  ,a.mnt_avalado 
from """+db_plat_tempX+""".tmp_ope_lc_libre_disposicion  a 
  left join """+db_plat_tempX+""".tmp_modelo_consumo b
		on a.periodo_id = b.periodo_id
  	and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
    and trim(b.cod_modelo) = 'STD'
where a.periodo_id = """+anomes+"""
  and a.cod_tipo_activo in (52, 53)
"""

# COMMAND ----------

sql_safe(query_ins_linea_credito_cons_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 5: Obtencion Operaciones Otras Líneas de crédito de libre disposición consumo Tipo Activo 54

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones contingentes de Líneas de crédito de libre disposición comerciales utilizando tablas tmp_d00_deuda_act_ctg (Salida de Notebook 1)y tabla de Provisión por operación del cliente (tbl_prov_gr_con_bci_ope_0722) para valores de modelo interno
# MAGIC
# MAGIC **Tabla de salida**: tmp_ope_lc_libre_disposicion(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente (Calificacion cliente)
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**:relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_linea_credito_otro_con = """ insert into """+db_plat_tempX+""".tmp_ope_lc_libre_disposicion  
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
	,a.mto_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
	,'' as cod_cta_ifrs
  ,a.fl_ope_reneg 
	,54 as cod_tipo_activo
	,'OTROS CONSUMO' as nombre_activo 
	,0 as pct_pi
	,0 as pct_pdi
	,0 as pct_pe
	,0 as mnt_provision
	,'GR' as cod_tipo_cli
	,0 as mnt_exposicion
	,0.1 as factor_expo
	,'No_Tiene' as tipo_garantia				
	,0 as mnt_garantia
	,0 as pct_ltv
	,round(val_pi/100,5) 		as pct_pi_metodo_interno
  ,round(val_pdi/100,5)   as pct_pdi_metodo_interno
  ,round(tas_ope/100,5)   as pct_pe_metodo_interno
	,b.Prv_tot_ctg  				as mnt_prov_metodo_interno
	,0 as mnt_avalado
from """+db_plat_tempX+""".tmp_d00_deuda_act_ctg a
	left join """+db_RiesgoCredX+""".tbl_prov_gr_con_bci_ope_0722 b
		on a.periodo_id = substring(b.fec_fpro, 1, 6)
  	and trim(a.cod_num_operacion) = trim(b.Operacion)
where a.periodo_id =  """+anomes+"""
 	and trim(a.cod_cartera_ope) = 'CTG'
 	and trim(a.cod_cctb) IN ( '931000063','931000048')
  and trim(a.fl_ope_reneg) <> 'C' 
 """

# COMMAND ----------

sql_safe(query_ins_linea_credito_otro_con)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 6: Actualizacion Valores de Líneas de crédito de libre disposición Consumo (Otros Consumo) desde Modelo Estandar

# COMMAND ----------

# MAGIC %md
# MAGIC se actualizan operaciones contingentes utilizando tablas tmp_ope_lc_libre_disposicion (Salida paso anterior, Tipo Activo 54) y tabla tmp_modelo_consumo (Salida Notebook 005) para valores de modelo estandar
# MAGIC
# MAGIC **Tabla de salida**: tmp_ope_lc_libre_disposicion_2(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente (Calificacion cliente)
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**:relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_linea_credito_otro_con_2 = """ insert into """+db_plat_tempX+""".tmp_ope_lc_libre_disposicion_2
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
  ,a.mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
  ,a.cod_cta_ifrs
  ,a.fl_ope_reneg
  ,a.cod_tipo_activo
  ,a.nombre_activo
  ,CASE WHEN trim(a.cod_cctb) = '931000048' THEN cast(round(b.pct_pi/100,5) as float)	ELSE 0 END AS pct_pi
  ,CASE WHEN trim(a.cod_cctb) = '931000048' THEN cast(round(b.pct_pdi/100,5) as float) ELSE 0 END AS pct_pdi
  ,CASE WHEN trim(a.cod_cctb) = '931000048' THEN cast(round(b.pct_pe/100,5) as float) ELSE 0 END AS pct_pe
  ,CASE WHEN trim(a.cod_cctb) = '931000048' THEN b.mnt_provtot_cont ELSE 0 END   as mnt_provision
  ,a.cod_tipo_cli
  ,CASE WHEN trim(a.cod_cctb) = '931000048' AND trim(a.ind_cdet) = 'N' THEN round(a.mnt_deuda_ope * 0.1,0) 
			WHEN trim(a.ind_cdet) = 'N' OR (trim(a.ind_cdet) = 'D' AND b.pct_pi <> 100) THEN round((a.mnt_deuda_ope * 0.4),0) 
			ELSE 0 
			END AS  mnt_exposicion
  ,CASE WHEN trim(a.cod_cctb) = '931000048' THEN 0.1 ELSE 0.4 END as factor_expo
  ,a.des_tipo_gtia
  ,a.mnt_garantia
  ,a.pct_ltv
  ,a.pct_pi_metodo_interno 
  ,a.pct_pdi_metodo_interno 
  ,a.pct_pe_metodo_interno 
  ,a.mnt_prov_metodo_interno 
  ,a.mnt_avalado 
from """+db_plat_tempX+""".tmp_ope_lc_libre_disposicion  a 
  left join """+db_plat_tempX+""".tmp_modelo_consumo b
	on a.periodo_id = b.periodo_id
  	and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
    and trim(b.cod_modelo) = 'STD'
where a.periodo_id = """+anomes+"""
  and a.cod_tipo_activo = 54 
""" 

# COMMAND ----------

sql_safe(query_ins_linea_credito_otro_con_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 7: Obtencion Operaciones Otros compromisos de crédito Tipo Activo 62

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones contingentes de Líneas de crédito de libre disposición comerciales utilizando tablas tmp_d00_deuda_act_ctg (Salida de Notebook 1)y tabla tmp_modelo_com_gr (Salida Notebook 003)
# MAGIC
# MAGIC **Tabla de salida**: tmp_ope_lc_libre_disposicion(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente (Calificacion cliente)
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**:relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_otros_compromisos = """ insert into """+db_plat_tempX+""".tmp_ope_lc_libre_disposicion  
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
	,a.mto_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
	,'' as cod_cta_ifrs
  ,a.fl_ope_reneg 
	,62 as cod_tipo_activo
	,'OTROS COMPROMISOS DE CREDITO' as nombre_activo 
	,b.pct_pi
	,b.pct_pdi
	,b.pct_pe
	,b.mnt_prov_oficial as mnt_provision
	,case when b.periodo_id is null then 'SIN' else 'GR' end as cod_tipo_cli
	,0 as mto_exposicion
	,1.0 as factor_expo
	,b.des_tipo_gtia as TIPO_GARANTIA				
	,b.mnt_garantia as MONTO_GARANTIA
	,b.pct_ltv as PTVG 
	,b.pct_pi_metodo_interno	
	,b.pct_pdi_metodo_interno	
	,b.pct_pe_metodo_interno	
	,b.mnt_prov_int as mto_PROV_METODO_INTERNO 
	,b.mnt_avalado as mto_avalado
from """+db_plat_tempX+""".tmp_d00_deuda_act_ctg a
	left join """+db_plat_tempX+""".tmp_modelo_com_gr b
		on a.periodo_id = b.periodo_id
  	and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
    and trim(a.cod_cartera_ope) = trim(b.cod_stp_cart)
where a.periodo_id = """+anomes+"""
 	and trim(a.cod_cartera_ope) = 'CTG'
 	and (trim(a.cod_cctb) IN ('900100103','931000014','931000023','931000032','931000039',
										 '931000045','931000055','931000056','931000057','931000058',
										 '931000059','931004250','931004251','939500010','939500012',
										 '931000052', /* Lineas de credito de proyectos 17/11/2022 */
										 '939500014') /* 628000025 ESTA INCORRECTA ESTA CCTB PARA LOS LEA	*/
        or trim(a.cod_num_operacion) like 'LEA%')
  and trim(a.fl_ope_reneg) <> 'C' 
order by trim(a.cod_cctb) 
"""

# COMMAND ----------

sql_safe(query_ins_otros_compromisos)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 8: Actualizacion Valores detros compromisos de crédito desde Modelo Individual

# COMMAND ----------

# MAGIC %md
# MAGIC se actualizan operaciones contingentes utilizando tablas tmp_ope_lc_libre_disposicion (Salida paso anterior) y tabla tmp_modelo_indiv (Salida Notebook 002) para valores de modelo interno y estandar para operaciones con provision mayor a 0
# MAGIC
# MAGIC **Tabla de salida**: tmp_ope_lc_libre_disposicion_2(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente (Calificacion cliente)
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**:relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado

# COMMAND ----------

query_ins_otros_compromisos_2 = """ insert into """+db_plat_tempX+""".tmp_ope_lc_libre_disposicion_2
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
  ,a.mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
  ,a.cod_cta_ifrs
  ,a.fl_ope_reneg
  ,a.cod_tipo_activo
  ,a.nombre_activo
  ,b.pct_pi
  ,b.pct_pdi
  ,b.pct_pe
  ,coalesce(a.mnt_provision, b.mnt_provision)  as mnt_provision
  ,CASE WHEN trim(a.cod_tipo_cli) = 'SIN' THEN b.cod_calificacion ELSE 'GR' END	as cod_tipo_cli
  ,round((CASE WHEN trim(a.ind_cdet) = 'N' OR (trim(a.ind_cdet) = 'D' AND coalesce(a.pct_pi, b.pct_pi) <> 1) THEN (a.mnt_deuda_ope * 1.0) ELSE 0 END),0) as mnt_exposicion
  ,a.factor_expo
  ,coalesce(a.des_tipo_gtia, b.cod_tipo_garantia) as des_tipo_gtia
  ,coalesce(a.mnt_garantia, b.mnt_garantia) as mnt_garantia
  ,coalesce(a.pct_ltv, 0)  as pct_ltv
  ,coalesce(a.pct_pi_metodo_interno, 0)   as pct_pi_metodo_interno
  ,coalesce(a.pct_pdi_metodo_interno, 0)  as pct_pdi_metodo_interno
  ,coalesce(a.pct_pe_metodo_interno, 0) as pct_pe_metodo_interno
  ,coalesce(a.mnt_prov_metodo_interno, 0) as mnt_prov_metodo_interno
  ,coalesce(a.mnt_avalado, 0) as mnt_avalado
from """+db_plat_tempX+""".tmp_ope_lc_libre_disposicion  a 
  --left join """+db_plat_tempX+""".tmp_modelo_indiv b 
  left join """+db_plat_tempX+""".tmp_bci15_modelo_ind_agrp b
    on a.periodo_id=b.periodo_id
    and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
    and b.mnt_provision <> 0
    --and trim(b.cod_tipo_deu) = 'CONT'
    and trim(a.cod_cartera_ope) = trim(b.cod_tipo_activo)
where a.periodo_id = """+anomes+"""
  and a.cod_tipo_activo = 62
"""

# COMMAND ----------

sql_safe(query_ins_otros_compromisos_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 9: Eliminación Líneas de crédito de libre disposición consumo y Comercial y Otros compromisos de crédito Conmsumo para fecha de Proceso

# COMMAND ----------

query_delete_avales = """DELETE FROM """ + db_platinumX + """.base_archivos_normativos WHERE periodo_id = """+anomes+""" and cod_tabla_89 in (51, 52, 53, 54, 62) and trim(cod_cartera_ope) = 'CTG' """

# COMMAND ----------

sql_safe(query_delete_avales)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 10: Insert Operaciones Líneas de crédito de libre disposición consumo y Comercial y Otros compromisos de crédito Conmsumo

# COMMAND ----------

# MAGIC %md
# MAGIC se actualizan operaciones contingentes utilizando tablas tmp_ope_avales_y_fia_2 (Salida paso 3) y tabla tmp_modelo_indiv (Salida Notebook 02) para valores de modelo interno y estandar
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_ope_original**: Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **cod_tip_cart**: Tipo cartera 
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Subtipo cartera
# MAGIC - **mto_deuda_ope**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cta_ifrs**: Cuenta IFRS
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **nombre_activo**: Nombre Activo Cartera
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada
# MAGIC - **mto_provision**: Prov Oficial
# MAGIC - **cod_tipo_cli**: Tipo Cliente (Calificacion cliente)
# MAGIC - **mto_exposicion**: Prov Oficial
# MAGIC - **factor_expo**: Porcentaje Factor Exposicion
# MAGIC - **des_tipo_gtia**: Tipo garantia		
# MAGIC - **mnt_garantia**: Monto Garantia
# MAGIC - **pct_ltv**:relación entre la deuda actual y el valor del bien al origen
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **mto_prov_metodo_interno**: Provisión de la Colocación
# MAGIC - **mnt_avalado**: Monto Avalado
# MAGIC - **fec_proceso**: Fecha Cierre

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

query_ins_linea_credito = """ insert into """ + db_platinumX + """.base_archivos_normativos 
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
  ,a.mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deteriodo_ope
  ,a.cod_cctb
  ,a.fl_ope_reneg
  ,-998                              as cod_tabla_34
  ,a.cod_tipo_activo
  ,a.nombre_activo
  ,a.pct_pi
  ,a.pct_pdi
  ,a.pct_pe
  ,a.mnt_provision
  ,a.cod_tipo_cli
  ,a.mnt_exposicion
  ,a.factor_expo
  ,a.des_tipo_gtia
  ,a.mnt_garantia
  ,a.pct_ltv
  ,a.pct_pi_metodo_interno
  ,a.pct_pdi_metodo_interno
  ,a.pct_pe_metodo_interno
  ,a.mnt_prov_metodo_interno
  ,a.mnt_avalado
 ,'"""+str(date_proceso)+"""'
from """+db_plat_tempX+""".tmp_ope_lc_libre_disposicion_2 a
where a.periodo_id = """+anomes+"""  
"""  

# COMMAND ----------

sql_safe(query_ins_linea_credito)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_platinumX + """.base_archivos_normativos  where periodo_id = """+anomes+""" and cod_tabla_89 in (51, 52, 53, 54, 62) """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal base_archivos_normativos ")

# COMMAND ----------

# MAGIC %md
# MAGIC # Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")