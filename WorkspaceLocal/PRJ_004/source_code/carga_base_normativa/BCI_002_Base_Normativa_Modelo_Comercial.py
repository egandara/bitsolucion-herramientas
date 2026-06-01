# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_002_Base_Normativa_Modelo_Comercial

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC **************************************************************************
# MAGIC * Nombre: BCI_002_Base_Normativa_Modelo_Comercial.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor: Jonathan Araya R. BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de registrar información de Operaciones de los modelos: Comercial Grupal, Comercial B1
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
# MAGIC * slv_RiesgoCred_RiesgoCredPer_db.prov_gr_com_b1_0719
# MAGIC * slv_RiesgoCred_RiesgoCredPer_db.prov_gr_com_bci_ope_1118
# MAGIC ***************************************************************************
# MAGIC #### Tabla Salida Temporal: 
# MAGIC * tmp_modelo_com_gr

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variables

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Captura de Variables

# COMMAND ----------

dbutils.widgets.removeAll()
dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso :")
dbutils.widgets.text("db_plat_tempW","","02 platinum temp db:")
dbutils.widgets.text("db_location_plat_tempW","","03 Location platinum temp db:")
dbutils.widgets.text("db_RiesgoCredW","","04 slv_RiesgoCred_RiesgoCredPer DB:")
dbutils.widgets.text("db_prv_comW","","05 provisionInternaConsumo:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)

db_RiesgoCredX = dbutils.widgets.get("db_RiesgoCredW")
spark.conf.set("bci.db_RiesgoCredX", db_RiesgoCredX)

db_prv_comX = dbutils.widgets.get("db_prv_comW")
spark.conf.set("bci.db_prv_comX", db_prv_comX)

print("fechaProcesoX: " + fechaProcesoX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_location_plat_tempX: " + db_location_plat_tempX)
print("db_RiesgoCredX: " + db_RiesgoCredX)
print("db_prv_comX: " + db_prv_comX)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Carga de funciones

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
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')
valida_param_vacio(db_RiesgoCredX,'db_RiesgoCredX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Base de Datos

# COMMAND ----------

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
# MAGIC ###Tabla tmp_prov_gr_com_b1_0719 

# COMMAND ----------

paso_tb_del_tmp_b1_0719  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_prov_gr_com_b1_0719"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_b1_0719)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_prov_gr_com_b1_0719", True)

# COMMAND ----------

paso_tb_crea_tmp_com_grp_b1_0719 = """CREATE TABLE """ + db_plat_tempX + """.tmp_prov_gr_com_b1_0719
(
	periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
	des_banco STRING COMMENT 'Banco',
	des_prestamo STRING COMMENT 'tipo de préstamo: Comercial, estudiantil o leasing',
	rut_cliente INT COMMENT 'rut cliente',
  dv_cliente STRING COMMENT 'dígito verificador cliente',
	cod_ope_num STRING COMMENT 'id operación',
	negocio_id INT COMMENT 'indicador de negocio',
	mnt_deuda_total_ifrs BIGINT COMMENT 'deuda total IFRS',
	num_dias_mora INT COMMENT 'días de mora operación',
	pct_pi DECIMAL(9,6) COMMENT 'probabilidad de incumplimiento del cliente',
	pct_pdi DECIMAL(9,6) COMMENT 'perdida dado el incumplimiento de la operación',
	pct_pe DECIMAL(9,6) COMMENT 'pérdida esperada',				
	mnt_prov_oficial DECIMAL(20,8) COMMENT 'provisión con mitigación',
	cod_cctb INT COMMENT 'código cuenta contable',
	cod_tipo_activo  STRING COMMENT 'tipo de crédito, activo o contingente',
	mnt_prov_int DECIMAL(20,8) COMMENT 'provisión',														
	mnt_avalada	 DECIMAL(20,8) COMMENT 'monto de mitigación',
	pct_ltv DECIMAL(20,8) COMMENT 'relación entre la deuda actual y el valor del bien al origen', 
	cod_tipo_gtia STRING COMMENT 'tipo de garantía',		
	mnt_ult_tas_mto_sum DECIMAL(20,8) COMMENT 'última Tasación, si es Tipo_Inmobiliario = 1 --> última tasación > 1 si no tasación más reciente'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_prov_gr_com_b1_0719' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_com_grp_b1_0719)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Tabla tmp_com_grp_b1

# COMMAND ----------

paso_tb_del_tmp_b1  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_com_grp_b1"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_b1)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_com_grp_b1", True)

# COMMAND ----------

paso_tb_crea_tmp_com_grp_b1 = """CREATE TABLE """ + db_plat_tempX + """.tmp_com_grp_b1
(
	periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
	des_banco STRING COMMENT 'Banco',
	des_prestamo STRING COMMENT 'tipo de préstamo: Comercial, estudiantil o leasing',
	rut_cliente INT COMMENT 'rut cliente',
  dv_cliente STRING COMMENT 'dígito verificador cliente',
	cod_ope_num STRING COMMENT 'id operación',
	negocio_id INT COMMENT 'indicador de negocio',
	mnt_deuda_total_ifrs BIGINT COMMENT 'deuda total IFRS',
	num_dias_mora INT COMMENT 'días de mora operación',
	pct_pi DECIMAL(9,6) COMMENT 'probabilidad de incumplimiento del cliente',
	pct_pdi DECIMAL(9,6) COMMENT 'perdida dado el incumplimiento de la operación',
	pct_pe DECIMAL(9,6) COMMENT 'pérdida esperada',				
	mnt_prov_oficial DECIMAL(20,8) COMMENT 'provisión con mitigación',
	cod_cctb INT COMMENT 'código cuenta contable',
	cod_tipo_activo  STRING COMMENT 'tipo de crédito, activo o contingente',
	mnt_prov_int DECIMAL(20,8) COMMENT 'provisión',														
	mnt_avalada	 DECIMAL(20,8) COMMENT 'monto de mitigación',
	pct_ltv DECIMAL(20,8) COMMENT 'relación entre la deuda actual y el valor del bien al origen', 
	cod_tipo_gtia STRING COMMENT 'tipo de garantía',		
	mnt_ult_tas_mto_sum DECIMAL(20,8) COMMENT 'última Tasación, si es Tipo_Inmobiliario = 1 --> última tasación > 1 si no tasación más reciente'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_com_grp_b1' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_com_grp_b1)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla tmp_mode_int 

# COMMAND ----------

paso_tb_del_tmp_mode_int  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_mode_int"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_mode_int)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_mode_int", True)

# COMMAND ----------

paso_tb_crea_tmp_mode_int  = """CREATE TABLE """ + db_plat_tempX + """.tmp_mode_int
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  cod_num_operacion STRING COMMENT 'Colocación',
  rut_cliente INT COMMENT 'RUT del Cliente Titular',
  dv_cliente STRING COMMENT 'Digito verificador del cliente titular',
  tipo_activo STRING COMMENT 'Codigo Tipo Activo',
  negocio_id STRING COMMENT 'negocio id',
  pct_pi DECIMAL(12,6) COMMENT 'Probabilidad Incumplimiento',
  pct_pdi DECIMAL(12,6) COMMENT 'Pérdida dado el incumplimiento del cliente (PDI)',
  pct_pe DECIMAL(12,6) COMMENT 'Pérdida Esperada'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_mode_int' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_mode_int)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Tabla tmp_mode_nova

# COMMAND ----------

paso_tb_del_tmp_mode_nova  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_mode_nova"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_mode_nova)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_mode_nova", True)

# COMMAND ----------

paso_tb_crea_tmp_mode_nova  = """CREATE TABLE """ + db_plat_tempX + """.tmp_mode_nova
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente INT COMMENT 'RUT del Cliente Titular',
  dv_cliente STRING COMMENT 'Digito verificador del cliente titular',
  cod_num_operacion STRING COMMENT 'Operación',
  pct_pi DECIMAL(12,6) COMMENT 'Probabilidad Incumplimiento',
  pct_pdi DECIMAL(12,6) COMMENT 'Pérdida dado el incumplimiento del cliente (PDI)',
  pct_pe DECIMAL(12,6) COMMENT 'Pérdida Esperada',
  des_tipo_credito STRING COMMENT 'Tipo de crédito'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_mode_nova' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_mode_nova)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla tmp_modelo_com_gr_v1 

# COMMAND ----------

paso_tb_del_tmp_modelo_com_gr1  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_modelo_com_gr_v1"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_modelo_com_gr1)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_modelo_com_gr_v1", True)

# COMMAND ----------

paso_tb_crea_tmp_modelo_com_gr1  = """CREATE TABLE """ + db_plat_tempX + """.tmp_modelo_com_gr_v1
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente INT COMMENT 'RUT del Cliente Titular',
  dv_cliente STRING COMMENT 'Digito verificador del cliente titular',
  cod_num_operacion STRING COMMENT 'Número Operación',
  des_banco String COMMENT 'Banco',
  des_prestamo String COMMENT 'PRESTAMO',
  deuda_total_ifrs DECIMAL(18,0) COMMENT 'Saldo total ifrs',
  num_dias_mora_ope INT COMMENT 'Dias de mora',
  pct_pi DECIMAL(12,6) COMMENT 'PI',
  pct_pdi DECIMAL(12,6) COMMENT 'PDI',
  pct_pe DECIMAL(12,6) COMMENT 'PE',
  pct_pi_metodo_interno DECIMAL(12,6) COMMENT 'PI metodo interno',
  pct_pdi_metodo_interno DECIMAL(12,6) COMMENT 'PDI metodo interno',
  pct_pe_metodo_interno DECIMAL(12,6) COMMENT 'PE metodo interno',
  prov_oficial DECIMAL(32,0) COMMENT 'Prov Oficial',
  cod_cctb STRING COMMENT 'Cuenta contable',
  cod_stp_cart STRING COMMENT 'Tipo cartera (OPE_VIGENTE) ',
  prov_int DECIMAL(32,0) COMMENT 'Prov B1 d00',
  mnt_avalado DECIMAL(32,0) COMMENT 'MONTO_AVALADO',
  pct_ltv DECIMAL(12,6) COMMENT 'PTVG',
  des_tipo_gtia STRING COMMENT 'Tipo garantia',
 	mnt_garantia DECIMAL(32,0) COMMENT 'MONTO_GARANTIA' 
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_modelo_com_gr_v1' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_modelo_com_gr1)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla tmp_modelo_com_gr 

# COMMAND ----------

paso_tb_del_tmp_modelo_com_gr  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_modelo_com_gr"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_modelo_com_gr)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_modelo_com_gr", True)

# COMMAND ----------

paso_tb_crea_tmp_modelo_com_gr  = """CREATE TABLE """ + db_plat_tempX + """.tmp_modelo_com_gr
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente INT COMMENT 'RUT del Cliente Titular',
  dv_cliente STRING COMMENT 'Digito verificador del cliente titular',
  cod_num_operacion STRING COMMENT 'Número Operación',
  des_banco String COMMENT 'Banco',
  des_prestamo String COMMENT 'PRESTAMO',
  deuda_total_ifrs DECIMAL(18,0) COMMENT 'Saldo total ifrs',
  num_dias_mora_ope INT COMMENT 'Dias de mora',
  pct_pi DECIMAL(12,6) COMMENT 'probabilidad de incumplimiento del cliente',
  pct_pdi DECIMAL(12,6) COMMENT 'perdida dado el incumplimiento de la operación',
  pct_pe DECIMAL(12,6) COMMENT 'pérdida esperada',
  pct_pi_metodo_interno DECIMAL(12,6) COMMENT 'PI metodo interno',
  pct_pdi_metodo_interno DECIMAL(12,6) COMMENT 'PDI metodo interno',
  pct_pe_metodo_interno DECIMAL(12,6) COMMENT 'PE metodo interno',
  mnt_prov_oficial DECIMAL(32,0) COMMENT 'Prov Oficial',
  cod_cctb STRING COMMENT 'Cuenta contable',
  cod_stp_cart STRING COMMENT 'Tipo cartera  Act/Ctg',
  mnt_prov_int DECIMAL(32,0) COMMENT 'Prov B1 d00',
  mnt_avalado DECIMAL(32,0) COMMENT 'MONTO_AVALADO',
  pct_ltv DECIMAL(12,6) COMMENT 'relación entre la deuda actual y el valor del bien al origen',
  des_tipo_gtia STRING COMMENT 'Tipo garantia',
 	mnt_garantia DECIMAL(32,0) COMMENT 'Monto Garantia' 
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_modelo_com_gr' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_modelo_com_gr)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Desarrollo Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 1: Obtencion Operaciones de Cartera Comercial Grupal B1

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista con Operaciones Comerciales B1 desde tabla slv_RiesgoCred_RiesgoCredPer_db.prov_gr_com_b1_0719
# MAGIC
# MAGIC **Tabla de salida**: tmp_prov_gr_com_b1_0719(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: periodo proceso formato SSAAMM
# MAGIC - **des_banco**: Descripcion Banco
# MAGIC - **des_prestamo**: tipo de préstamo: Comercial, estudiantil o leasing
# MAGIC - **rut_cliente**: rut cliente
# MAGIC - **dv_cliente**: dígito verificador cliente
# MAGIC - **cod_ope_num**: id operación
# MAGIC - **negocio_id**: indicador de negocio
# MAGIC - **mnt_deuda_total_ifrs**: deuda total IFRS
# MAGIC - **num_dias_mora**: días de mora operación
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada		
# MAGIC - **mnt_prov**: provisión con mitigación
# MAGIC - **cod_cctb**: código cuenta contable
# MAGIC - **cod_tipo_activo**: tipo de crédito, activo o contingente
# MAGIC - **mnt_prov_int**: monto provisión									
# MAGIC - **mnt_avalada**: monto de mitigación
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **cod_tipo_gtia**: tipo de garantía
# MAGIC - **mnt_ult_tas_mto_sum**: última Tasación, si es Tipo_Inmobiliario = 1 --> última tasación > 1 si no tasación más reciente

# COMMAND ----------

query_com_b1 = """ insert into """ + db_plat_tempX + """.tmp_prov_gr_com_b1_0719   
select  """+anomes+""" 	as PERIODO_ID
		,b1.BANCO
		,b1.PRESTAMO
		,b1.rut                          as rut_cliente
    ,b1.dv                           as dv_cliente
		,b1.ope_num                      as operacion
		,b1.Negocio_Id
		,b1.deuda_totalIFRS
		,b1.dias_mora
		,b1.PI	
		,b1.PDI	
		,b1.PE					
--		,B1.PROV_Oficial	ML 11-11-2024 Se modifica registro ya que solo considera metodo estandar como prov oficial
    ,b1.PROV_cm as PROV_Oficial
		,b1.d00_cod_cctb
		,b1.d00_cod_stp_cart
		,b1.prov_d00 as PROV_Int														
		,ROUND(cast(b1.MONTO_AVALADO as DECIMAL(20,8)),0) AS MONTO_AVALADO
		,cast(b1.ptvg as DECIMAL(20,8))  as ptvg 
		,b1.Tipo_Gtia 		
		,cast(b1.ult_tas_mto_sum as DECIMAL(20,8))  as ult_tas_mto_sum
from """+ db_RiesgoCredX +""".prov_gr_com_b1_0719 b1
where  periodo = """+anomes+"""
and trim(b1.prestamo) NOT IN ('FACTORING')
  """

# COMMAND ----------

sql_safe(query_com_b1)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 2: Actualizacion Campo Prov Int, se obtiene desde tabla prov_gr_com_bci_ope_1118

# COMMAND ----------

# MAGIC %md
# MAGIC se Actualiza campo prov_int desde tabla slv_RiesgoCred_RiesgoCredPer_db.prov_gr_com_bci_ope_1118 para tabla temporal tmp_prov_gr_com_b1_0719 generada en Paso 1
# MAGIC
# MAGIC **Tabla de salida**: tmp_com_gr_b1(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: periodo proceso formato SSAAMM
# MAGIC - **des_banco**: Descripcion Banco
# MAGIC - **des_prestamo**: tipo de préstamo: Comercial, estudiantil o leasing
# MAGIC - **rut_cliente**: rut cliente
# MAGIC - **dv_cliente**: dígito verificador cliente
# MAGIC - **cod_ope_num**: id operación
# MAGIC - **negocio_id**: indicador de negocio
# MAGIC - **mnt_deuda_total_ifrs**: deuda total IFRS
# MAGIC - **num_dias_mora**: días de mora operación
# MAGIC - **pct_pi**: probabilidad de incumplimiento del cliente
# MAGIC - **pct_pdi**: perdida dado el incumplimiento de la operación
# MAGIC - **pct_pe**: pérdida esperada		
# MAGIC - **mnt_prov**: provisión con mitigación
# MAGIC - **cod_cctb**: código cuenta contable
# MAGIC - **cod_tipo_activo**: tipo de crédito, activo o contingente
# MAGIC - **mnt_prov_int**: monto con la Provisión de Operaciones Comercial Modelo Interno Personas							
# MAGIC - **mnt_avalada**: monto de mitigación
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen
# MAGIC - **cod_tipo_gtia**: tipo de garantía
# MAGIC - **mnt_ult_tas_mto_sum**: última Tasación, si es Tipo_Inmobiliario = 1 --> última tasación > 1 si no tasación más reciente

# COMMAND ----------

query_com_b1_final = """ insert into """ + db_plat_tempX + """.tmp_com_grp_b1  
select a.periodo_id
,a.des_banco
,a.des_prestamo
,a.rut_cliente
,a.dv_cliente
,a.cod_ope_num
,a.negocio_id
,a.mnt_deuda_total_ifrs
,a.num_dias_mora
,a.pct_pi
,a.pct_pdi
,a.pct_pe
,a.mnt_prov_oficial
,a.cod_cctb
,a.cod_tipo_activo
,CASE WHEN b.operacion IS NULL THEN a.mnt_prov_int
      ELSE b.provision END      as mnt_prov_int
,a.mnt_avalada
,a.pct_ltv
,a.cod_tipo_gtia
,a.mnt_ult_tas_mto_sum
from """ + db_plat_tempX + """.tmp_prov_gr_com_b1_0719 a
left join """+db_RiesgoCredX+""".prov_gr_com_bci_ope_1118 b
  on a.periodo_id =substring(b.fechaproceso,7,4)||substring(b.fechaproceso,4,2)
  and trim(a.cod_ope_num) = trim(b.operacion)
  and a.rut_cliente = b.rut
where  a.periodo_id = """+anomes+"""
"""

# COMMAND ----------

# MAGIC %md
# MAGIC sql_safe(query_com_b1_final)

# COMMAND ----------

qry_modelo_pov_int = """ create or replace temporary view tmp_modelo_provision as 
    SELECT """+anomes+"""          as periodo
				,operacion                 as operacion
        ,rut                       as rut_cliente
        ,digitoverificador         as dv_cliente        
        ,case when conversion = 'N' then 'ACT' when conversion = 'C' then 'CTG' end as tipo_Activo
        ,segmento                   as segmento
        ,sum(provision)            as provision
		FROM  """+db_RiesgoCredX+""".prov_gr_com_bci_ope_1118 
    WHERE substring(fechaproceso,7,4)||substring(fechaproceso,4,2)||substring(fechaproceso,1,2) = """+fechaProcesoX+"""
   /* and segmento_producto = 'BCI' and origen_deuda = 'BCI' and sub_tipo_cartera <> 'CAE' */
		GROUP by operacion
      ,rut
      ,digitoverificador
      ,case when conversion = 'N' then 'ACT' when conversion = 'C' then 'CTG' end
      ,segmento  """ 
  
sql_safe(qry_modelo_pov_int)

# COMMAND ----------

qry_ins_tmp_com_grp_b1 = """ insert into """+db_plat_tempX+""".tmp_com_grp_b1  
select a.periodo_id
    ,a.des_banco
    ,a.des_prestamo
    ,a.rut_cliente
    ,a.dv_cliente
    ,a.cod_ope_num
    ,a.negocio_id
    ,a.mnt_deuda_total_ifrs
    ,a.num_dias_mora
    ,a.pct_pi
    ,a.pct_pdi
    ,a.pct_pe
    ,a.mnt_prov_oficial
    ,a.cod_cctb
    ,a.cod_tipo_activo
    ,coalesce( b.provision ,0)      as mnt_prov_int
    ,a.mnt_avalada
    ,a.pct_ltv
    ,a.cod_tipo_gtia
    ,a.mnt_ult_tas_mto_sum
from """+db_plat_tempX+""".tmp_prov_gr_com_b1_0719 a
left join tmp_modelo_provision b
  on a.periodo_id =b.periodo
  and trim(a.cod_ope_num) = trim(b.operacion)
  and a.rut_cliente  = b.rut_cliente
  and  trim(a.cod_tipo_activo) = trim(tipo_Activo)
where  a.periodo_id = """+anomes+"""
"""

sql_safe(qry_ins_tmp_com_grp_b1)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 3: Obtencion Operaciones Comercial Modelo Interno Personas.

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista con Operaciones Comercial Modelo Interno Personas desde tabla slv_RiesgoCred_RiesgoCredPer_db.prov_gr_com_bci_ope_1118
# MAGIC
# MAGIC **Tabla de salida**: tmp_mode_int(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: periodo proceso formato SSAAMM
# MAGIC - **cod_num_operacion**: Numero Operacion
# MAGIC - **rut_cliente**: RUT del Cliente Titular',
# MAGIC - **dv_cliente**: Digito verificador del cliente titular',
# MAGIC - **negocio_id**: negocio id',
# MAGIC - **pct_pi**: Probabilidad Incumplimiento
# MAGIC - **pct_pdi**: Pérdida dado el incumplimiento del cliente (PDI)
# MAGIC - **pct_pe**: Pérdida Esperada

# COMMAND ----------

query_tmp_mode_int = """ insert into """ + db_plat_tempX + """.tmp_mode_int  
select """+anomes+"""		         							as periodo
	,a.operacion   	  	    	       							as operacion
  ,a.rut          	        	     							as rut_cliente
  ,a.digitoverificador        		 							as dv_cliente   
	,case when a.conversion = 'N' then 'ACT' 
				when a.conversion = 'C' then 'CTG' end 	as tipo_activo
  ,'Negocio_id'             									as Negocio_id
  ,a.pi 		                  									as pi_cli
	,a.pdi    		        												as pdi_ope
	,a.pe 																				as pe_ope
	/*,sum(a.pi)                 									as pi_cli
	,sum(a.pdi)            												as pdi_ope
	,sum(a.pe) 																		as pe_ope */
FROM  """+db_RiesgoCredX+""".prov_gr_com_bci_ope_1118 a
WHERE substring(fechaproceso,7,4)||substring(fechaproceso,4,2)||substring(fechaproceso,1,2) = """+fechaProcesoX+"""
	AND trim(tipo) = 'CLI'
/* group by a.operacion  
  ,a.rut   
  ,a.digitoverificador   
	,case when a.conversion = 'N' then 'ACT' 
				when a.conversion = 'C' then 'CTG' end*/
"""

# COMMAND ----------

sql_safe(query_tmp_mode_int)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 4: Obtencion Operaciones NOVA

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista con Operaciones Comercial Nova desde tabla de Proceso Comercial Grupal Interno prv_com_int_nova_com_migr
# MAGIC
# MAGIC **Tabla de salida**: tmp_mode_nova(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: periodo proceso formato SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular',
# MAGIC - **dv_cliente**: Digito verificador del cliente titular',
# MAGIC - **cod_num_operacion**: Numero Operacion
# MAGIC - **pct_pi**: Probabilidad Incumplimiento
# MAGIC - **pct_pdi**: Pérdida dado el incumplimiento del cliente (PDI)
# MAGIC - **pct_pe**: Pérdida Esperada
# MAGIC - **des_tipo_credito**: Tipo de Credito (Comercial)

# COMMAND ----------

query_tmp_mode_nova = """ insert into """ + db_plat_tempX + """.tmp_mode_nova
select """+anomes+"""               as periodo_id
    ,cliente_rut                    as rut_cliente
    ,cliente_dv                     as cli_dv
    ,num_operacion                  as num_ope
    ,format_number(pct_pi, 6)       as PI
    ,format_number(pct_pdi, 6)      as PDI
    ,format_number(pct_pe, 6)       as PE
    ,'COMERCIAL'                    as tipo_credito
from  """+db_prv_comX+""".prv_com_int_nova_com_migr
where  id_periodo = """+anomes+"""
"""

# COMMAND ----------

sql_safe(query_tmp_mode_nova)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 5: Obtencion Garantia Comercial Individual

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista con Operaciones Comerciales grupales estandar e interna, identificando sus valores de pi tanto b1 como modelo interno de Bci y/o Nova, este Paso es el resultado del cruce de los 3 pasos previos
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_com_gr_v1(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: periodo proceso formato SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **des_banco**: Banco
# MAGIC - **des_prestamo**: PRESTAMO
# MAGIC - **deuda_total_ifrs**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **pct_pi**: PI
# MAGIC - **pct_pdi**: PDI
# MAGIC - **pct_pe**: PE
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **prov_oficial**: Prov Oficial
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_stp_cart**: Tipo cartera (OPE_VIGENTE) 
# MAGIC - **prov_int**: Prov B1 d00
# MAGIC - **mnt_avalado**: MONTO_AVALADO
# MAGIC - **pct_ltv**: PTVG
# MAGIC - **des_tipo_gtia**: Tipo garantia
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **mnt_garantia**: MONTO_GARANTIA

# COMMAND ----------

query_tmp_mode_gr_int = """ insert into """ + db_plat_tempX + """.tmp_modelo_com_gr_v1
SELECT b1.periodo_id
		,b1.rut_cliente
		,b1.dv_cliente
		,b1.cod_ope_num
		,b1.des_banco
		,b1.des_prestamo
		,b1.mnt_deuda_total_ifrs
		,b1.num_dias_mora
		,b1.pct_pi	
		,b1.pct_pdi	
		,b1.pct_pe		
		/*,coalesce(mode_int.pct_pi ,mode_nova.pct_pi ) 							as pi_metodo_interno	
		,coalesce(mode_int.pct_pdi,mode_nova.pct_pdi) 							as pdi_metodo_interno	
		,coalesce(mode_int.pct_pe ,(mode_nova.pct_pi * mode_nova.pct_pdi))			as pe_metodo_interno	*/
		,mode_int.pct_pi			as pi_metodo_interno
		,mode_int.pct_pdi			as pdi_metodo_interno	
		,mode_int.pct_pe			as pdi_metodo_interno
		,b1.mnt_prov_oficial	
		,b1.cod_cctb
		,b1.cod_tipo_activo
		,b1.mnt_prov_int														as prov_metodo_interno
		,round(b1.mnt_avalada,0) as monto_avalado
		,b1.pct_ltv 
		,case when b1.cod_tipo_gtia = '' then 'no_tiene' else b1.cod_tipo_gtia end		as tipo_garantia
		,b1.mnt_ult_tas_mto_sum													as monto_garantia 
	FROM """ + db_plat_tempX + """.tmp_com_grp_b1 b1
	LEFT JOIN """ + db_plat_tempX + """.tmp_mode_int MODE_INT
			on b1.periodo_id = mode_int.periodo_id
			and b1.cod_ope_num = mode_int.cod_num_operacion
			--and = b1.negocio_id = mode_int.negocio_id
      and b1.cod_tipo_activo = mode_int.tipo_activo
WHERE b1.periodo_id = """+anomes+"""   """

# COMMAND ----------

sql_safe(query_tmp_mode_gr_int )

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 6: Actualizacion Perdida al incumplimiento Metodo Interno para Clientes Nova

# COMMAND ----------

# MAGIC %md
# MAGIC Actaulizacion Tabla Temporal tmp_modelo_com_gr_v1 con Operaciones Comerciales grupales estandar e interna, identificando sus valores de pi tanto b1 para modelo interno de Clientes Nova, Cruces Pasos 5 y $ respectivamente
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_com_gr(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: periodo proceso formato SSAAMM
# MAGIC - **rut_cliente**: RUT del Cliente Titular
# MAGIC - **dv_cliente**: Digito verificador del cliente titular
# MAGIC - **cod_num_operacion**: Número Operación
# MAGIC - **des_banco**: Banco
# MAGIC - **des_prestamo**: PRESTAMO
# MAGIC - **deuda_total_ifrs**: Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **pct_pi**: PI
# MAGIC - **pct_pdi**: PDI
# MAGIC - **pct_pe**: PE
# MAGIC - **pct_pi_metodo_interno**: PI metodo interno
# MAGIC - **pct_pdi_metodo_interno**: PDI metodo interno
# MAGIC - **pct_pe_metodo_interno**: PE metodo interno
# MAGIC - **prov_oficial**: Prov Oficial
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_stp_cart**: Tipo cartera ACT/CTG
# MAGIC - **prov_int**: Prov B1 d00
# MAGIC - **mnt_avalado**: MONTO_AVALADO
# MAGIC - **pct_ltv**: PTVG
# MAGIC - **des_tipo_gtia**: Tipo garantia
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00
# MAGIC - **mnt_garantia**: MONTO_GARANTIA

# COMMAND ----------

query_tmp_mode_gr_int_nova = """ insert into """ + db_plat_tempX + """.tmp_modelo_com_gr
SELECT b1.periodo_id
		,b1.rut_cliente
		,b1.dv_cliente
		,b1.cod_num_operacion
		,b1.des_banco
		,b1.des_prestamo
		,b1.deuda_total_ifrs
		,b1.num_dias_mora_ope
		,b1.pct_pi	
		,b1.pct_pdi	
		,b1.pct_pe		
		,coalesce(b1.pct_pi_metodo_interno ,mode_nova.pct_pi ) 							            as pct_pi_metodo_interno	
		,coalesce(b1.pct_pdi_metodo_interno,mode_nova.pct_pdi) 							            as pct_pdi_metodo_interno	
		,coalesce(b1.pct_pe_metodo_interno ,(mode_nova.pct_pi * mode_nova.pct_pdi))			as pct_pe_metodo_interno	
		,b1.prov_oficial	
		,b1.cod_cctb
		,b1.cod_stp_cart
		,b1.prov_int														as prov_metodo_interno
		,b1.mnt_avalado                             as monto_avalado
		,b1.pct_ltv 
		,b1.des_tipo_gtia
		,b1.mnt_garantia													  as monto_garantia
	FROM """ + db_plat_tempX + """.tmp_modelo_com_gr_v1 b1
	LEFT JOIN """ + db_plat_tempX + """.tmp_mode_nova mode_nova
			on b1.cod_num_operacion = mode_nova.cod_num_operacion
      and b1.periodo_id = mode_nova.periodo_id 
WHERE b1.periodo_id = """+anomes+"""   """

# COMMAND ----------

sql_safe(query_tmp_mode_gr_int_nova )

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida

# COMMAND ----------

registros = spark.sql(f"""
    SELECT count(1) AS cant_registros
    FROM {db_plat_tempX}.tmp_modelo_com_gr
    WHERE periodo_id = {anomes}
""")

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal tmp_modelo_com_gr ")

# COMMAND ----------

# MAGIC %md
# MAGIC # Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")